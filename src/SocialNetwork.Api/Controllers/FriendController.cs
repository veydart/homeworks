using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Application.Interfaces;
using SocialNetwork.Domain.Interfaces;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Route("friend")]
[Authorize]
public class FriendController : ControllerBase
{
    private readonly IFriendRepository _friendRepo;
    private readonly IFeedCacheService _feedCache;

    public FriendController(IFriendRepository friendRepo, IFeedCacheService feedCache)
    {
        _friendRepo = friendRepo;
        _feedCache = feedCache;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPut("set/{user_id}")]
    public async Task<IActionResult> Set(Guid user_id)
    {
        await _friendRepo.AddAsync(GetUserId(), user_id);
        // Инвалидируем кеш ленты — он перестроится при следующем запросе
        await _feedCache.InvalidateFeedAsync(GetUserId());
        return Ok();
    }

    [HttpPut("delete/{user_id}")]
    public async Task<IActionResult> Delete(Guid user_id)
    {
        await _friendRepo.DeleteAsync(GetUserId(), user_id);
        await _feedCache.InvalidateFeedAsync(GetUserId());
        return Ok();
    }
}
