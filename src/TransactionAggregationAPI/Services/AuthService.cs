using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Capitec_Transaction_Aggregation_API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext dbContext, IJwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email and password are required.");

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.LastLoginDate = DateTime.UtcNow;
        user.LastUpdatedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully with role {Role}", user.UserName, user.Role);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.UserName,
            Role = user.Role.ToString()
        };
    }
}