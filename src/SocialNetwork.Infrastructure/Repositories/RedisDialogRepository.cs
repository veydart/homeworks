using System.Text.Json;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;
using StackExchange.Redis;

namespace SocialNetwork.Infrastructure.Repositories;

public class RedisDialogRepository : IDialogRepository
{
    private readonly IConnectionMultiplexer _redis;

    private static readonly LuaScript SendScript = LuaScript.Prepare(
        "redis.call('ZADD', @key, @score, @message) return 1");

    private static readonly LuaScript GetDialogScript = LuaScript.Prepare(
        "return redis.call('ZRANGEBYSCORE', @key, '-inf', '+inf')");

    public RedisDialogRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SendAsync(DialogMessage message)
    {
        var db = _redis.GetDatabase();
        var key = $"dialog:{message.DialogKey}";
        var json = JsonSerializer.Serialize(new
        {
            id = message.Id,
            from = message.FromUserId,
            to = message.ToUserId,
            text = message.Text,
            createdAt = message.CreatedAt
        });

        await db.ScriptEvaluateAsync(SendScript, new
        {
            key = (RedisKey)key,
            score = (double)message.CreatedAt.Ticks,
            message = (RedisValue)json
        });
    }

    public async Task<List<DialogMessage>> GetDialogAsync(string dialogKey)
    {
        var db = _redis.GetDatabase();
        var key = $"dialog:{dialogKey}";

        var result = (RedisValue[])await db.ScriptEvaluateAsync(GetDialogScript, new
        {
            key = (RedisKey)key
        });

        var messages = new List<DialogMessage>();
        if (result == null) return messages;

        foreach (var item in result)
        {
            var doc = JsonDocument.Parse(item.ToString());
            messages.Add(new DialogMessage
            {
                Id = Guid.Parse(doc.RootElement.GetProperty("id").GetString()!),
                DialogKey = dialogKey,
                FromUserId = Guid.Parse(doc.RootElement.GetProperty("from").GetString()!),
                ToUserId = Guid.Parse(doc.RootElement.GetProperty("to").GetString()!),
                Text = doc.RootElement.GetProperty("text").GetString()!,
                CreatedAt = doc.RootElement.GetProperty("createdAt").GetDateTime()
            });
        }

        return messages;
    }
}
