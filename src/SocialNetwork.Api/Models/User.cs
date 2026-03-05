namespace SocialNetwork.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Interests { get; set; }
    public string? City { get; set; }
}
