using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Capitec_Transaction_Aggregation_API.Services;

public class AuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext dbContext, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Username and password are required.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == request.Username || u.Email == request.Username);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        user.LastLoginDate = DateTime.UtcNow;
        user.LastUpdatedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully with role {Role}", request.Username, user.Role);

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.UserName,
            Role = user.Role.ToString()
        };
    }

    private string GenerateJwtToken(Models.User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var expiresAt = DateTime.UtcNow.AddHours(8);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
    }
}