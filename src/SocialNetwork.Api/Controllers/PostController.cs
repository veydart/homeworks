using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Application.DTOs;
using SocialNetwork.Application.Services;

namespace SocialNetwork.Api.Controllers;

[ApiController]
[Route("post")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly PostService _postService;

    public PostController(PostService postService) => _postService = postService;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var postId = await _postService.CreateAsync(GetUserId(), request);
        return Ok(new { postId });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdatePostRequest request)
    {
        await _postService.UpdateAsync(GetUserId(), request);
        return Ok();
    }

    [HttpPut("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _postService.DeleteAsync(GetUserId(), id);
        return Ok();
    }

    [HttpGet("get/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id)
    {
        var post = await _postService.GetAsync(id);
        if (post is null) return NotFound();
        return Ok(post);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 10,
        [FromServices] FeedService feedService = null!)
    {
        var feed = await feedService.GetFeedAsync(GetUserId(), offset, limit);
        return Ok(feed);
    }
}
