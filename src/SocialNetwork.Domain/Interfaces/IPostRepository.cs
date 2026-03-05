using SocialNetwork.Domain.Entities;

namespace SocialNetwork.Domain.Interfaces;

public interface IPostRepository
{
    Task<Guid> CreateAsync(Post post);
    Task<Post?> GetByIdAsync(Guid id);
    Task UpdateAsync(Post post);
    Task DeleteAsync(Guid id);
    Task<List<Post>> GetPostsByIdsAsync(List<Guid> ids);
    Task<List<Post>> GetFeedFromDbAsync(Guid userId, int offset, int limit);
}
