# SocialNetwork API

Скелет социальной сети на .NET 10 + PostgreSQL.

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (запущенный на localhost:5432)

## Настройка базы данных

1. Убедитесь, что PostgreSQL запущен.
2. Создайте базу данных:

```sql
CREATE DATABASE social_network;
```

Таблица `users` создаётся автоматически при запуске приложения.

## Запуск

```bash
dotnet run --project src/SocialNetwork.Api
```

Приложение запустится на `http://localhost:5000`.

## Swagger UI

Откройте в браузере: [http://localhost:5000/swagger](http://localhost:5000/swagger)

## API

| Метод | URL | Описание |
|-------|-----|----------|
| POST | `/user/register` | Регистрация пользователя |
| POST | `/login` | Авторизация (получение JWT-токена) |
| GET | `/user/get/{id}` | Получение анкеты по ID |

### Пример регистрации

```json
POST /user/register
{
  "firstName": "Иван",
  "lastName": "Иванов",
  "birthDate": "1990-01-15",
  "gender": "male",
  "interests": "Программирование, Музыка",
  "city": "Москва",
  "password": "secret123"
}
```

### Пример авторизации

```json
POST /login
{
  "id": "<user_id из регистрации>",
  "password": "secret123"
}
```

## Postman

Коллекция: `postman/SocialNetwork.postman_collection.json`

Импортируйте в Postman и выполняйте запросы по порядку:
1. Register User → автоматически сохраняет `userId`
2. Login → автоматически сохраняет `token`
3. Get User by ID → использует сохранённый `userId`
