using SocialNetwork.Application.DTOs;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;

namespace SocialNetwork.Application.Services;

public class DialogService
{
    private readonly IDialogRepository _dialogRepo;

    public DialogService(IDialogRepository dialogRepo) => _dialogRepo = dialogRepo;

    public static string BuildDialogKey(Guid user1, Guid user2)
    {
        var a = user1.ToString();
        var b = user2.ToString();
        return string.Compare(a, b, StringComparison.Ordinal) < 0
            ? $"{a}_{b}"
            : $"{b}_{a}";
    }

    public async Task SendAsync(Guid fromUserId, Guid toUserId, string text)
    {
        var message = new DialogMessage
        {
            Id = Guid.NewGuid(),
            DialogKey = BuildDialogKey(fromUserId, toUserId),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        await _dialogRepo.SendAsync(message);
    }

    public async Task<List<DialogMessageDto>> GetDialogAsync(Guid currentUserId, Guid otherUserId)
    {
        var dialogKey = BuildDialogKey(currentUserId, otherUserId);
        var messages = await _dialogRepo.GetDialogAsync(dialogKey);

        return messages.Select(m => new DialogMessageDto
        {
            From = m.FromUserId,
            To = m.ToUserId,
            Text = m.Text
        }).ToList();
    }
}
