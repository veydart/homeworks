namespace SocialNetwork.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
