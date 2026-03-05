using Microsoft.AspNetCore.Mvc;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Services;

namespace SocialNetwork.Api.Controllers;

[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;

    public UserController(IUserRepository userRepository, PasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("/user/register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest("FirstName and LastName are required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required.");

        var hash = _passwordHasher.Hash(request.Password);
        var userId = await _userRepository.CreateAsync(request, hash);

        return Ok(new RegisterResponse { UserId = userId });
    }

    [HttpGet("/user/get/{id}")]
    public async Task<ActionResult<User>> Get(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(user);
    }
}
