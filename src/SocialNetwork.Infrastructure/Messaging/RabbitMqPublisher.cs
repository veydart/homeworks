using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SocialNetwork.Application.Interfaces;

namespace SocialNetwork.Infrastructure.Messaging;

public class RabbitMqPublisher : IPostEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "post.feed";

    private RabbitMqPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(string hostName = "localhost")
    {
        var factory = new ConnectionFactory { HostName = hostName };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true);

        return new RabbitMqPublisher(connection, channel);
    }

    public async Task PublishPostCreatedAsync(Guid postId, Guid authorId, string text, List<Guid> followerIds)
    {
        var message = new
        {
            postId,
            authorId,
            text,
            createdAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        foreach (var followerId in followerIds)
        {
            var props = new BasicProperties { Persistent = true };
            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: followerId.ToString(),
                mandatory: false,
                basicProperties: props,
                body: body);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
