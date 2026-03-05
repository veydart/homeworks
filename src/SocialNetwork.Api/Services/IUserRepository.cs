using SocialNetwork.Api.Models;

namespace SocialNetwork.Api.Services;

public interface IUserRepository
{
    Task<Guid> CreateAsync(RegisterRequest request, string passwordHash);
    Task<User?> GetByIdAsync(Guid id);
    Task<string?> GetPasswordHashByIdAsync(Guid id);
}
