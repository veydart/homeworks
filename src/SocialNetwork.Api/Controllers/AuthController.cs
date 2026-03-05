using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Services;

namespace SocialNetwork.Api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(IUserRepository userRepository, PasswordHasher passwordHasher, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    [HttpPost("/login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var hash = await _userRepository.GetPasswordHashByIdAsync(request.Id);
        if (hash is null)
            return NotFound("User not found.");

        if (!_passwordHasher.Verify(request.Password, hash))
            return Unauthorized("Invalid password.");

        var token = GenerateJwtToken(request.Id);
        return Ok(new LoginResponse { Token = token });
    }

    private string GenerateJwtToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "SuperSecretKeyForSocialNetwork2024!@#$"));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "SocialNetwork",
            audience: _configuration["Jwt:Audience"] ?? "SocialNetwork",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
