# Отчёт: ДЗ 5 — Масштабируемая подсистема диалогов

## 1. Архитектура

```
                    ┌─────────────────────┐
                    │   .NET 10 API       │
                    │  DialogController   │
                    │  DialogService      │
                    └─────────┬───────────┘
                              │
                    ┌─────────▼───────────┐
                    │  Citus Coordinator  │  (порт 5432)
                    │  PostgreSQL + Citus │
                    └─────────┬───────────┘
                              │ Шардирование по dialog_key
                    ┌─────────┴─────────┐
                    ▼                   ▼
           ┌──────────────┐    ┌──────────────┐
           │ Citus Worker │    │ Citus Worker │
           │      1       │    │      2       │
           └──────────────┘    └──────────────┘
```

## 2. Ключ шардирования

**`DialogKey = min(user1, user2) + "_" + max(user1, user2)`**

Детерминированный ключ, вычисляемый из пары пользователей, гарантирует:

1. **Все сообщения одного диалога на одном шарде** — SELECT и INSERT работают в пределах одного узла, без cross-shard запросов
2. **«Эффект Леди Гаги»** — распределение идёт по парам, а не по одному пользователю. Даже если Леди Гага отправляет 100,000 сообщений разным людям, каждый диалог будет на отдельном шарде

```csharp
public static string BuildDialogKey(Guid user1, Guid user2)
{
    var a = user1.ToString();
    var b = user2.ToString();
    return string.Compare(a, b, StringComparison.Ordinal) < 0
        ? $"{a}_{b}" : $"{b}_{a}";
}
```

## 3. Распределение таблицы

```sql
-- На координаторе после EF миграции:
SELECT create_distributed_table('DialogMessages', 'DialogKey');
```

Citus автоматически создаёт 32 шарда и распределяет их между воркерами.

## 4. Решардинг без даунтайма

### Процедура добавления нового воркера

```bash
# 1. Запустить новый воркер (docker compose scale или новый контейнер)
docker compose up -d citus-worker-3

# 2. Зарегистрировать на координаторе
psql -h localhost -U postgres -d social_network \
  -c "SELECT citus_add_node('citus-worker-3', 5432);"

# 3. Запустить онлайн-ребалансировку
psql -h localhost -U postgres -d social_network \
  -c "SELECT citus_rebalance_start();"

# 4. Проверить статус
psql -h localhost -U postgres -d social_network \
  -c "SELECT * FROM citus_rebalance_status();"
```

### Как это работает

1. **`citus_add_node`** — регистрирует новый воркер в кластере
2. **`citus_rebalance_start`** — запускает фоновый процесс перемещения шардов
3. Citus **не блокирует** запросы во время ребалансировки:
   - Перемещает шарды по одному
   - Использует logical replication для копирования данных
   - Переключает маршрутизацию атомарно после завершения копирования
4. **Даунтайм = 0** — приложение продолжает работать

## 5. API

### POST /dialog/{user_id}/send

```json
// Request
{ "text": "Привет, как дела?" }

// Response: 200 OK
```

### GET /dialog/{user_id}/list

```json
// Response: 200 OK
[
  { "from": "uuid1", "to": "uuid2", "text": "Привет!" },
  { "from": "uuid2", "to": "uuid1", "text": "Нормально, а ты?" }
]
```

## 6. Запуск

```bash
# 1. Поднять кластер
docker compose up -d

# 2. Дождаться инициализации (citus-manager добавит воркеры)
docker compose logs citus-manager

# 3. Запустить приложение (EF миграция применится автоматически)
dotnet run --project src/SocialNetwork.Api

# 4. Распределить таблицу
psql -h localhost -U postgres -d social_network \
  -f docker/citus/distribute-table.sql
```

## 7. Файлы

| Файл | Назначение |
|---|---|
| `Domain/Entities/DialogMessage.cs` | Сущность сообщения |
| `Domain/Interfaces/IDialogRepository.cs` | Контракт репозитория |
| `Application/Services/DialogService.cs` | Бизнес-логика + DialogKey |
| `Infrastructure/Repositories/DialogRepository.cs` | EF Core реализация |
| `Api/Controllers/DialogController.cs` | REST endpoints |
| `docker-compose.yml` | Citus кластер + Redis |
| `docker/citus/distribute-table.sql` | Распределение таблицы |
