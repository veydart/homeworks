namespace SocialNetwork.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Interests { get; set; }
    public string? City { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public List<Post> Posts { get; set; } = [];
    public List<Friendship> Friends { get; set; } = [];
}
