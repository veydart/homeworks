using SocialNetwork.Application.DTOs;
using SocialNetwork.Application.Interfaces;
using SocialNetwork.Domain.Interfaces;

namespace SocialNetwork.Application.Services;

public class FeedService
{
    private readonly IFeedCacheService _feedCache;
    private readonly IPostRepository _postRepo;

    public FeedService(IFeedCacheService feedCache, IPostRepository postRepo)
    {
        _feedCache = feedCache;
        _postRepo = postRepo;
    }

    public async Task<List<PostDto>> GetFeedAsync(Guid userId, int offset, int limit)
    {
        // Пытаемся получить из Redis
        var postIds = await _feedCache.GetFeedAsync(userId, offset, limit);

        List<Domain.Entities.Post> posts;

        if (postIds.Count > 0)
        {
            // Кеш есть — получаем посты по ID
            posts = await _postRepo.GetPostsByIdsAsync(postIds);
        }
        else if (offset == 0)
        {
            // Кеш пуст и мы на первой странице — fallback на БД
            posts = await _postRepo.GetFeedFromDbAsync(userId, offset, limit);
        }
        else
        {
            posts = [];
        }

        return posts.Select(p => new PostDto
        {
            Id = p.Id,
            Text = p.Text,
            AuthorUserId = p.AuthorId,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
