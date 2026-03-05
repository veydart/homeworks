namespace SocialNetwork.Domain.Entities;

public class Friendship
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid FriendId { get; set; }
    public User Friend { get; set; } = null!;
}
