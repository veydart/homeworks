using SocialNetwork.Application.Interfaces;
using StackExchange.Redis;

namespace SocialNetwork.Infrastructure.Cache;

public class RedisFeedCacheService : IFeedCacheService
{
    private readonly IDatabase _redis;
    private const int MaxFeedSize = 1000;

    public RedisFeedCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    private static string FeedKey(Guid userId) => $"feed:{userId}";

    public async Task AddToFeedsAsync(Guid postId, List<Guid> followerIds)
    {
        var postIdStr = postId.ToString();
        foreach (var followerId in followerIds)
        {
            var key = FeedKey(followerId);
            await _redis.ListLeftPushAsync(key, postIdStr);
            await _redis.ListTrimAsync(key, 0, MaxFeedSize - 1);
        }
    }

    public async Task<List<Guid>> GetFeedAsync(Guid userId, int offset, int limit)
    {
        var key = FeedKey(userId);
        var values = await _redis.ListRangeAsync(key, offset, offset + limit - 1);

        return values
            .Where(v => v.HasValue)
            .Select(v => Guid.Parse(v.ToString()))
            .ToList();
    }

    public async Task InvalidateFeedAsync(Guid userId)
    {
        await _redis.KeyDeleteAsync(FeedKey(userId));
    }

    public async Task RemovePostFromFeedsAsync(Guid postId, List<Guid> followerIds)
    {
        var postIdStr = postId.ToString();
        foreach (var followerId in followerIds)
        {
            await _redis.ListRemoveAsync(FeedKey(followerId), postIdStr);
        }
    }
}
