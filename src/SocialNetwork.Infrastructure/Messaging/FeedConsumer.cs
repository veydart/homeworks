using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SocialNetwork.Application.Interfaces;

namespace SocialNetwork.Infrastructure.Messaging;

public class FeedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<FeedConsumer> _logger;
    private readonly string _rabbitHost;
    private IConnection? _connection;
    private IChannel? _channel;

    public FeedConsumer(
        IServiceScopeFactory scopeFactory,
        WebSocketConnectionManager wsManager,
        ILogger<FeedConsumer> logger,
        string rabbitHost = "localhost")
    {
        _scopeFactory = scopeFactory;
        _wsManager = wsManager;
        _logger = logger;
        _rabbitHost = rabbitHost;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _rabbitHost };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: "post.feed",
                    type: ExchangeType.Direct,
                    durable: true,
                    cancellationToken: stoppingToken);

                var queueResult = await _channel.QueueDeclareAsync(
                    queue: "feed.updates",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await _channel.QueueBindAsync(
                    queue: queueResult.QueueName,
                    exchange: "post.feed",
                    routingKey: "#",
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var routingKey = ea.RoutingKey;

                        if (Guid.TryParse(routingKey, out var targetUserId))
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var feedCache = scope.ServiceProvider.GetRequiredService<IFeedCacheService>();

                            var postEvent = JsonSerializer.Deserialize<PostEvent>(body);
                            if (postEvent != null)
                            {
                                await feedCache.AddToFeedsAsync(postEvent.postId, [targetUserId]);
                                await _wsManager.SendToUserAsync(targetUserId, body);
                            }
                        }

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing feed event");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: queueResult.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("FeedConsumer started listening");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FeedConsumer connection error, retrying in 5s...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private record PostEvent(Guid postId, Guid authorId, string text, DateTime createdAt);
}
