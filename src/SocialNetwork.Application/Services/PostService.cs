using SocialNetwork.Application.DTOs;
using SocialNetwork.Application.Interfaces;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;

namespace SocialNetwork.Application.Services;

public class PostService
{
    private readonly IPostRepository _postRepo;
    private readonly IFriendRepository _friendRepo;
    private readonly IFeedCacheService _feedCache;

    public PostService(IPostRepository postRepo, IFriendRepository friendRepo, IFeedCacheService feedCache)
    {
        _postRepo = postRepo;
        _friendRepo = friendRepo;
        _feedCache = feedCache;
    }

    public async Task<Guid> CreateAsync(Guid authorId, CreatePostRequest request)
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Text = request.Text,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        };

        var postId = await _postRepo.CreateAsync(post);

        // Обновляем кеш лент всех подписчиков (друзей автора)
        var followerIds = await _friendRepo.GetFollowerIdsAsync(authorId);
        if (followerIds.Count > 0)
            await _feedCache.AddToFeedsAsync(postId, followerIds);

        return postId;
    }

    public async Task<PostDto?> GetAsync(Guid id)
    {
        var post = await _postRepo.GetByIdAsync(id);
        if (post is null) return null;

        return new PostDto
        {
            Id = post.Id,
            Text = post.Text,
            AuthorUserId = post.AuthorId,
            CreatedAt = post.CreatedAt
        };
    }

    public async Task UpdateAsync(Guid authorId, UpdatePostRequest request)
    {
        var post = await _postRepo.GetByIdAsync(request.Id);
        if (post is null || post.AuthorId != authorId)
            throw new InvalidOperationException("Пост не найден или нет прав");

        post.Text = request.Text;
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepo.UpdateAsync(post);
    }

    public async Task DeleteAsync(Guid authorId, Guid postId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post is null || post.AuthorId != authorId)
            throw new InvalidOperationException("Пост не найден или нет прав");

        await _postRepo.DeleteAsync(postId);

        // Удаляем пост из кешей подписчиков
        var followerIds = await _friendRepo.GetFollowerIdsAsync(authorId);
        if (followerIds.Count > 0)
            await _feedCache.RemovePostFromFeedsAsync(postId, followerIds);
    }
}
