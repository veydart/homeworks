using SocialNetwork.Domain.Entities;

namespace SocialNetwork.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(User user);
    Task<User?> GetByIdWithPasswordAsync(Guid id);
}
