namespace SocialNetwork.Application.DTOs;

public class DialogMessageDto
{
    public Guid From { get; set; }
    public Guid To { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
}
