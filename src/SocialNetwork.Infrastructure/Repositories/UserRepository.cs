using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;
using SocialNetwork.Infrastructure.Data;

namespace SocialNetwork.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id)
        => await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Guid> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<User?> GetByIdWithPasswordAsync(Guid id)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
}
