namespace SocialNetwork.Application.DTOs;

public class PostDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
