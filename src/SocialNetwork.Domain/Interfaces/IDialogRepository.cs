using SocialNetwork.Domain.Entities;

namespace SocialNetwork.Domain.Interfaces;

public interface IDialogRepository
{
    Task SendAsync(DialogMessage message);
    Task<List<DialogMessage>> GetDialogAsync(string dialogKey);
}
