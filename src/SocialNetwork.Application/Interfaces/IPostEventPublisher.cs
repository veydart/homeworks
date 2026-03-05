namespace SocialNetwork.Application.Interfaces;

public interface IPostEventPublisher
{
    Task PublishPostCreatedAsync(Guid postId, Guid authorId, string text, List<Guid> followerIds);
}
