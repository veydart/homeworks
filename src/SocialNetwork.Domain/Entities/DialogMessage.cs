namespace SocialNetwork.Domain.Entities;

public class DialogMessage
{
    public Guid Id { get; set; }
    public string DialogKey { get; set; } = string.Empty;
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
