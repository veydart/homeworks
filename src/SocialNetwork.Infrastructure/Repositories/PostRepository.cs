using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;
using SocialNetwork.Infrastructure.Data;

namespace SocialNetwork.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _db;

    public PostRepository(AppDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(Post post)
    {
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return post.Id;
    }

    public async Task<Post?> GetByIdAsync(Guid id)
        => await _db.Posts.FirstOrDefaultAsync(p => p.Id == id);

    public async Task UpdateAsync(Post post)
    {
        _db.Posts.Update(post);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is not null)
        {
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<Post>> GetPostsByIdsAsync(List<Guid> ids)
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        // Сохраняем порядок из кеша
        var lookup = posts.ToDictionary(p => p.Id);
        return ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToList();
    }

    public async Task<List<Post>> GetFeedFromDbAsync(Guid userId, int offset, int limit)
    {
        // Получаем ID друзей
        var friendIds = await _db.Friendships
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendId)
            .ToListAsync();

        if (friendIds.Count == 0)
            return [];

        return await _db.Posts
            .AsNoTracking()
            .Where(p => friendIds.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }
}
