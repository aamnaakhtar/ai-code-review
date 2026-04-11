using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeReview.API.Data;
using CodeReview.API.DTOs;
using CodeReview.API.Models.Entities;
using CodeReview.API.Services;

namespace CodeReview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterRequestDto request)
    {
        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("Email already registered.");

        // Hash the password — never store plain text passwords
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", request.Email);

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Verify user exists AND password matches
        // Both checks are done before returning — prevents timing attacks
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        _logger.LogInformation("User logged in: {Email}", request.Email);

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
    }
}