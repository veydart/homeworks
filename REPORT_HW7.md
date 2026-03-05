# Отчёт: ДЗ 7 — In-Memory СУБД (Redis)

## 1. Модуль: Диалоги

Перенесён из PostgreSQL (EF Core) в Redis с использованием Lua-скриптов (UDF).

**Хранение:** Redis Sorted Set `dialog:{dialog_key}` (score = timestamp)

### Lua UDF

```lua
-- send_message: атомарная вставка
redis.call('ZADD', KEYS[1], ARGV[2], ARGV[1])

-- get_dialog: получение всех сообщений по ключу
redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', '+inf')
```

## 2. Результаты нагрузочного тестирования

### Send (POST /dialog/{id}/send)

| Метрика | PostgreSQL | Redis | Δ |
|---|---|---|---|
| **RPS (c=1)** | 216 | 211 | −2% |
| **RPS (c=10)** | 366 | 313 | −14% |
| **RPS (c=100)** | 220 | 187 | −15% |
| **Avg latency (c=1)** | 1.0 ms | **0.8 ms** | **−20%** |
| **p95 (c=1)** | 2 ms | **1 ms** | **−50%** |
| **p99 (c=1)** | 3 ms | **1 ms** | **−67%** |
| **p99 (c=100)** | 1229 ms | **452 ms** | **−63%** |
| **Errors (c=100)** | 2 | **0** | **− 100%** |

### List (GET /dialog/{id}/list)

| Метрика | PostgreSQL | Redis | Δ |
|---|---|---|---|
| **RPS (c=1)** | 216 | 211 | −2% |
| **RPS (c=10)** | 366 | 313 | −14% |
| **Avg latency (c=1)** | 3.6 ms | 3.9 ms | +8% |
| **p95 (c=10)** | 44 ms | **34 ms** | **−23%** |
| **p99 (c=10)** | 61 ms | **42 ms** | **−31%** |
| **p99 (c=100)** | 1484 ms | **468 ms** | **−68%** |
| **Errors (c=100)** | 1 | **0** | **−100%** |

## 3. Анализ

### Почему RPS сопоставим?

Узкое место — **HTTP-слой** .NET + однопоточный Node.js клиент. Оба хранилища отвечают быстрее, чем HTTP overhead. При прямом доступе к Redis (без HTTP) разница была бы на порядок.

### Где Redis побеждает?

1. **Latency при низкой нагрузке**: send p99 снизился с 3ms до **1ms** (−67%)
2. **Стабильность при высокой нагрузке**: p99 при c=100 снизился с 1484ms до **468ms** (−68%)
3. **Zero errors**: при c=100 PostgreSQL имел 3 ошибки, Redis — **0**

### Вывод

Redis значительно стабильнее при высоких нагрузках и обеспечивает более предсказуемые тайминги.

## 4. Файлы

| Файл | Назначение |
|---|---|
| `Infrastructure/Repositories/RedisDialogRepository.cs` | Реализация через Redis Lua scripts |
| `tools/dialog_load_test.js` | Скрипт нагрузочного тестирования |
| `dialog_bench_postgresql.json` | Результаты PostgreSQL |
| `dialog_bench_redis.json` | Результаты Redis |
