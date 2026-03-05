# Команды, выполненные при выполнении ДЗ 3

## 1. Подготовка ветки
```bash
# Создание ветки homework_3 от homework_2
git checkout -b homework_3
```

## 2. Сборка проекта
```bash
# Сборка после добавления /user/search и read/write split
dotnet build
```

## 3. Запуск Docker-кластера
```bash
# Поднятие кластера: 1 master (5440) + 2 slaves (5441, 5442)
docker compose up -d

# Остановка и пересоздание (если нужно пересобрать)
docker compose down
docker volume rm first_pg-slave1-data first_pg-slave2-data
docker compose up -d
```

## 4. Проверка репликации
```bash
# Проверка количества строк на мастере
docker exec pg-master psql -U postgres -d social_network -c "SELECT COUNT(*) FROM users;"

# Проверка количества строк на слейвах (должно быть 1,000,000)
docker exec pg-slave-1 psql -U postgres -d social_network -c "SELECT COUNT(*) FROM users;"
docker exec pg-slave-2 psql -U postgres -d social_network -c "SELECT COUNT(*) FROM users;"
```

## 5. Добавление индекса (реплицируется автоматически на слейвы)
```bash
docker exec pg-master psql -U postgres -d social_network -c \
  "CREATE INDEX IF NOT EXISTS idx_users_first_last_name ON users (first_name varchar_pattern_ops, last_name varchar_pattern_ops);"
```

## 6. Запуск приложения
```bash
# Запуск с подключением к Docker-кластеру (appsettings.json указывает на 5440/5441/5442)
dotnet run --project src/SocialNetwork.Api

# Или запуск с одиночной БД (без слейвов — fallback на master)
dotnet run --project src/SocialNetwork.Api -- --ConnectionStrings:Slave1="" --ConnectionStrings:Slave2=""
```

## 7. Проверка работы API
```bash
curl -s 'http://localhost:5000/user/search?first_name=%D0%90%D0%BB&last_name=%D0%98%D0%B2' | head -c 200
```

## 8. Нагрузочное тестирование
```bash
# Тест без репликации (одиночная БД)
node tools/load_test.js single_db

# Тест с репликацией (2 слейва)
node tools/load_test.js with_replication
```

## 9. Тест отказоустойчивости (failover)
```bash
# 1. Запуск нагрузки на запись (10 конкурентных потоков, 15 секунд)
node tools/failover_test.js 10 15

# 2. Через ~5 секунд — убить мастер (в отдельном терминале)
docker stop pg-master

# 3. Проверить LSN слейвов (кто свежее)
MSYS_NO_PATHCONV=1 docker exec pg-slave-1 psql -U postgres -d social_network -c "SELECT pg_last_wal_replay_lsn();"
MSYS_NO_PATHCONV=1 docker exec pg-slave-2 psql -U postgres -d social_network -c "SELECT pg_last_wal_replay_lsn();"

# 4. Промоутить свежий слейв до мастера
MSYS_NO_PATHCONV=1 docker exec pg-slave-1 gosu postgres pg_ctl promote -D /var/lib/postgresql/data

# 5. Подсчитать записанные тестовые строки (сравнить с success из failover_test)
MSYS_NO_PATHCONV=1 docker exec pg-slave-1 psql -U postgres -d social_network \
  -c "SELECT COUNT(*) FILTER (WHERE first_name LIKE 'Test%') as test_rows FROM users;"
```

## 10. Переключение полусинхронной репликации
```bash
# Включить на мастере (до остановки)
docker exec pg-master psql -U postgres -c \
  "ALTER SYSTEM SET synchronous_standby_names = 'FIRST 1 (slave_1_slot, slave_2_slot)'; SELECT pg_reload_conf();"

# Выключить (вернуть асинхронную)
docker exec pg-master psql -U postgres -c \
  "ALTER SYSTEM SET synchronous_standby_names = ''; SELECT pg_reload_conf();"
```

## 11. Коммит и пуш
```bash
git add .
git commit -m "feat: homework 3 - PostgreSQL replication"
git push -u origin homework_3
```

## Примечание: MSYS_NO_PATHCONV=1
На Windows в Git Bash пути вида `/var/lib/...` автоматически преобразуются
в `C:/Program Files/Git/var/lib/...`. Переменная `MSYS_NO_PATHCONV=1` отключает это.
