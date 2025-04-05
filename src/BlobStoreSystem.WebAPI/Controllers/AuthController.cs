using BlobStoreSystem.Domain.Entities;
using BlobStoreSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BlobStoreSystem.WebAPI.Controllers;

//[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly BlobStoreDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public record RegisterRequest(string Username, string Password);
    public record LoginRequest(string Username, string Password);

    public AuthController(BlobStoreDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    // POST: AuthController/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 1. Check if username already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
            return BadRequest("Username already taken.");

        // 2. Hash the password
        var passwordHash = ComputeSha256Hash(request.Password);

        // 3. Create new User entity
        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash
        };
        await _dbContext.Users.AddAsync(newUser);
        await _dbContext.SaveChangesAsync();

        // 4. Create a root directory for this new user
        var rootDir = new DirectoryNode
        {
            Name = "/",
            Path = "/",
            ParentDirectoryId = null,
            UserId = newUser.Id,
            Size = 0,
            MimeType = "inode/directory",
            CreatedBy = newUser.Id.ToString(),
            UpdatedBy = newUser.Id.ToString()
        };
        await _dbContext.Directories.AddAsync(rootDir);
        await _dbContext.SaveChangesAsync();


        return Ok(new { message = "User registered successfully" });
    }

    // POST: AuthController/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Find user
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
            return Unauthorized("Invalid username or password.");

        // 2. Check password
        var requestPasswordHash = ComputeSha256Hash(request.Password);
        if (user.PasswordHash != requestPasswordHash)
            return Unauthorized("Invalid username or password.");

        // 3. Generate JWT
        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private string GenerateJwtToken(User user)
    {
        // 1. Create claims
        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            };

        // 2. Get secret from appsettings.json
        var secretKey = _configuration["Authentication:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new Exception("JWT Secret Key not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Create token
        var token = new JwtSecurityToken(
            issuer: _configuration["Authentication:Issuer"],
            audience: _configuration["Authentication:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
