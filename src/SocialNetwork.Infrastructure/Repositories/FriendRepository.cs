using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Interfaces;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Infrastructure.Data;

namespace SocialNetwork.Infrastructure.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _db;

    public FriendRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Guid userId, Guid friendId)
    {
        var exists = await _db.Friendships.AnyAsync(f => f.UserId == userId && f.FriendId == friendId);
        if (!exists)
        {
            _db.Friendships.Add(new Friendship { UserId = userId, FriendId = friendId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid userId, Guid friendId)
    {
        var friendship = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);
        if (friendship is not null)
        {
            _db.Friendships.Remove(friendship);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
        => await _db.Friendships
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendId)
            .ToListAsync();

    // Кто добавил данного пользователя в друзья (обратная связь для push-модели)
    public async Task<List<Guid>> GetFollowerIdsAsync(Guid userId)
        => await _db.Friendships
            .Where(f => f.FriendId == userId)
            .Select(f => f.UserId)
            .ToListAsync();
}
