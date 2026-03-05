# Отчёт: ДЗ 6 — Онлайн-обновление ленты новостей

## 1. Архитектура

```
POST /post/create ──► PostService ──► RabbitMQ (exchange: post.feed)
                                         │
                                         │ routing_key = friend_user_id
                                         ▼
                                    FeedConsumer (BackgroundService)
                                         │
                          ┌──────────────┼──────────────┐
                          ▼              ▼              ▼
                     Redis cache    WebSocket push   (каждому
                     (лента)        (online users)    подписчику)
```

## 2. Компоненты

### RabbitMQ Publisher

При создании поста `PostService` публикует событие `PostCreated` в exchange `post.feed` (тип: direct).
Для **каждого друга** автора отправляется отдельное сообщение с `routing_key = friend_user_id`.

### FeedConsumer (BackgroundService)

Фоновый сервис, подписанный на очередь `feed.updates`:
1. Получает событие из RabbitMQ
2. Обновляет Redis-кеш ленты подписчика (`IFeedCacheService`)
3. Если пользователь онлайн — отправляет пост через WebSocket

Это реализует **отложенную материализацию** — формирование ленты происходит асинхронно через очередь.

### WebSocket endpoint

`/post/feed/posted?token=<JWT>` — подключение по WebSocket с авторизацией через JWT.

`WebSocketConnectionManager` — потокобезопасный менеджер подключений:
- `ConcurrentDictionary<Guid, ConcurrentBag<WebSocket>>`
- Один пользователь может иметь несколько активных подключений

## 3. Routing Key

Events отправляются **только целевым пользователям** через `routing_key = friend_user_id`. Это означает:

- Каждый друг автора получает своё сообщение
- RabbitMQ маршрутизирует по direct exchange
- Нет широковещательной рассылки — только адресная доставка

## 4. Запуск

```bash
# Поднять инфраструктуру
docker compose up -d

# Запустить приложение
dotnet run --project src/SocialNetwork.Api

# Проверить RabbitMQ Management UI
# http://localhost:15672 (guest/guest)
```

### Тестирование WebSocket

```javascript
// В консоли браузера после получения JWT токена
const ws = new WebSocket('ws://localhost:5000/post/feed/posted?token=YOUR_JWT');
ws.onmessage = (e) => console.log('Новый пост:', JSON.parse(e.data));
```

## 5. Файлы

| Файл | Назначение |
|---|---|
| `Infrastructure/Messaging/RabbitMqPublisher.cs` | Публикация событий в RabbitMQ |
| `Infrastructure/Messaging/FeedConsumer.cs` | BackgroundService: очередь → Redis + WebSocket |
| `Infrastructure/Messaging/WebSocketConnectionManager.cs` | Менеджер WebSocket подключений |
| `Application/Interfaces/IPostEventPublisher.cs` | Абстракция над publisher |
| `Application/Services/PostService.cs` | Публикация через RabbitMQ вместо прямого Redis |
| `Api/Program.cs` | WebSocket endpoint + DI |
| `docker-compose.yml` | + RabbitMQ |
