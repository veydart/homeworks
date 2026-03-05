namespace SocialNetwork.Application.DTOs;

public class CreatePostRequest
{
    public string Text { get; set; } = string.Empty;
}

public class UpdatePostRequest
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Interests { get; set; }
    public string? City { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public Guid Id { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Interests { get; set; }
    public string? City { get; set; }
}
