using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Application.DTOs;
using SocialNetwork.Application.Services;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Route("dialog")]
[Authorize]
public class DialogController : ControllerBase
{
    private readonly DialogService _dialogService;

    public DialogController(DialogService dialogService) => _dialogService = dialogService;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{user_id}/send")]
    public async Task<IActionResult> Send(Guid user_id, [FromBody] SendMessageRequest request)
    {
        await _dialogService.SendAsync(GetUserId(), user_id, request.Text);
        return Ok();
    }

    [HttpGet("{user_id}/list")]
    public async Task<IActionResult> List(Guid user_id)
    {
        var messages = await _dialogService.GetDialogAsync(GetUserId(), user_id);
        return Ok(messages);
    }
}
