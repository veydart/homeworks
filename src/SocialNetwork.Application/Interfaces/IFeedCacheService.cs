namespace SocialNetwork.Application.Interfaces;

public interface IFeedCacheService
{
    Task AddToFeedsAsync(Guid postId, List<Guid> followerIds);
    Task<List<Guid>> GetFeedAsync(Guid userId, int offset, int limit);
    Task InvalidateFeedAsync(Guid userId);
    Task RemovePostFromFeedsAsync(Guid postId, List<Guid> followerIds);
}
