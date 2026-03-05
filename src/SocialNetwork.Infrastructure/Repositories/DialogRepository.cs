using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Entities;
using SocialNetwork.Domain.Interfaces;
using SocialNetwork.Infrastructure.Data;

namespace SocialNetwork.Infrastructure.Repositories;

public class DialogRepository : IDialogRepository
{
    private readonly AppDbContext _db;

    public DialogRepository(AppDbContext db) => _db = db;

    public async Task SendAsync(DialogMessage message)
    {
        _db.DialogMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public async Task<List<DialogMessage>> GetDialogAsync(string dialogKey)
    {
        return await _db.DialogMessages
            .Where(m => m.DialogKey == dialogKey)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
