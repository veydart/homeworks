namespace SocialNetwork.Domain.Interfaces;

public interface IFriendRepository
{
    Task AddAsync(Guid userId, Guid friendId);
    Task DeleteAsync(Guid userId, Guid friendId);
    Task<List<Guid>> GetFriendIdsAsync(Guid userId);
    Task<List<Guid>> GetFollowerIdsAsync(Guid userId);
}
