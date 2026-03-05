# Отчёт: ДЗ 3 — Репликация PostgreSQL

## 1. Архитектура

### Схема репликации

```
                 ┌──────────────┐
    Запись ──────► pg-master    │  (порт 5432)
                 │  PostgreSQL  │
                 └──────┬───────┘
                        │ WAL Stream
              ┌─────────┴─────────┐
              ▼                   ▼
     ┌──────────────┐    ┌──────────────┐
     │ pg-slave-1   │    │ pg-slave-2   │
     │  (порт 5433) │    │  (порт 5434) │
     └──────────────┘    └──────────────┘
              ▲                   ▲
              └─────────┬─────────┘
                        │
    Чтение ─────────────┘ (round-robin)
```

### Read/Write разделение в приложении

- **Запись** (`POST /user/register`) → **master** через `NpgsqlDataSource` с ключом `"master"`
- **Чтение** (`GET /user/get/{id}`, `GET /user/search`) → **slave** через `SlaveDataSourcePool` (round-robin)
- Если слейвы не настроены — fallback на master

---

## 2. Замеры без репликации (одиночная БД)

Запрос: `GET /user/search?first_name=Ал&last_name=Ив` (c индексом из ДЗ 2)

### Latency (одиночная БД)

| Конкурентность | Avg (ms) | Median (ms) | P95 (ms) | P99 (ms) | Max (ms) |
|:-:|:-:|:-:|:-:|:-:|:-:|
| 1 | 1.2 | 0.9 | 2.4 | 2.8 | 115.3 |
| 10 | 1.7 | 1.6 | 2.3 | 3.0 | 80.8 |
| 100 | 18.8 | 17.9 | 24.2 | 29.9 | 455.8 |
| 1000 | 197.5 | 187.4 | 215.7 | 739.8 | 1,253.0 |

### Throughput (одиночная БД)

| Конкурентность | Запросов | RPS | Ошибки |
|:-:|:-:|:-:|:-:|
| 1 | 8,170 | 817 | 0 |
| 10 | 58,431 | 5,843 | 0 |
| 100 | 53,088 | 5,309 | 0 |
| 1000 | 50,893 | 5,089 | 0 |

---

## 3. Настройка репликации

### Docker Compose

Кластер запускается одной командой:

```bash
docker compose up -d
```

Конфигурация: `docker-compose.yml` — 3 контейнера PostgreSQL 16.

### Инициализация мастера (`docker/master/init-master.sh`)

```sql
-- postgresql.conf
wal_level = replica
max_wal_senders = 4
max_replication_slots = 4
hot_standby = on

-- Создание пользователя и слотов
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator_pass';
SELECT pg_create_physical_replication_slot('slave_1_slot');
SELECT pg_create_physical_replication_slot('slave_2_slot');
```

### Инициализация слейвов (`docker/slave/start-slave*.sh`)

1. Ожидание готовности мастера
2. `pg_basebackup` с мастера
3. Настройка `primary_conninfo` и `primary_slot_name`
4. Запуск в режиме `hot_standby`

---

## 4. Замеры с репликацией

> Для проведения замеров необходимо:
> 1. Запустить Docker: `docker compose up -d`
> 2. Дождаться инициализации (seed 1M строк ~2 мин)
> 3. Запустить приложение: `dotnet run --project src/SocialNetwork.Api`
> 4. Запустить тест: `node tools/load_test.js with_replication`

Ожидаемые улучшения при чтении со слейвов:
- Нагрузка на чтение распределяется между 2 слейвами
- Мастер разгружается для записей
- При высокой конкурентности (100+) RPS должен вырасти за счёт дополнительных ресурсов

---

## 5. Режимы репликации

### Асинхронная репликация (по умолчанию)

Мастер не ждёт подтверждения от слейвов. Максимальная производительность, но возможна потеря данных при аварии.

```sql
-- На мастере (по умолчанию)
synchronous_commit = on  -- но без synchronous_standby_names = асинхронная
```

### Полусинхронная репликация

Мастер ждёт подтверждения от минимум 1 слейва перед коммитом. Гарантирует, что данные записаны хотя бы на одну реплику.

```sql
-- На мастере
synchronous_standby_names = 'FIRST 1 (slave_1_slot, slave_2_slot)'
```

Переключение:
```bash
docker exec pg-master bash -c "echo \"synchronous_standby_names = 'FIRST 1 (slave_1_slot, slave_2_slot)'\" >> /var/lib/postgresql/data/postgresql.conf && pg_ctl reload -D /var/lib/postgresql/data"
```

---

## 6. Тест отказоустойчивости

### Процедура

1. Запустить нагрузку на запись:
```bash
node tools/failover_test.js 10 30
```

2. Через ~10 секунд убить мастер:
```bash
docker stop pg-master
```

3. Остановить нагрузку (Ctrl+C)

4. Проверить LSN слейвов:
```bash
docker exec pg-slave-1 psql -U postgres -c "SELECT pg_last_wal_replay_lsn();"
docker exec pg-slave-2 psql -U postgres -c "SELECT pg_last_wal_replay_lsn();"
```

5. Промоутить самый свежий слейв:
```bash
docker exec pg-slave-1 bash -c "pg_ctl promote -D /var/lib/postgresql/data"
```

6. Переключить второй слейв на нового мастера:
```bash
docker exec pg-slave-2 bash -c "
  sed -i 's/pg-master/pg-slave-1/' /var/lib/postgresql/data/postgresql.conf
  pg_ctl restart -D /var/lib/postgresql/data
"
```

7. Обновить приложение — изменить `Master` connection string на порт 5433.

8. Подсчитать потерянные транзакции:
```sql
-- На новом мастере
SELECT COUNT(*) FROM users WHERE first_name LIKE 'Test%';
-- Сравнить с количеством success из failover_test
```

### Ожидаемые результаты

| Режим | Потеря данных | Производительность записи |
|---|---|---|
| Асинхронный | Возможна (несколько транзакций) | Максимальная |
| Полусинхронный | Минимальна (0-1 транзакция) | Чуть ниже |

---

## 7. Файлы проекта

| Файл | Назначение |
|---|---|
| `docker-compose.yml` | Кластер: master + 2 slaves |
| `docker/master/init-master.sh` | Инициализация мастера (WAL, replication user, slots) |
| `docker/slave/start-slave*.sh` | Инициализация слейвов (pg_basebackup, streaming) |
| `src/.../Services/SlaveDataSourcePool.cs` | Round-robin пул слейвов |
| `src/.../Services/UserRepository.cs` | Read/Write split |
| `tools/load_test.js` | Нагрузочное тестирование чтения |
| `tools/failover_test.js` | Нагрузочное тестирование записи для failover |
