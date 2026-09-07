using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace TransactionAggregationAPI.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsJwtToken()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateAdminUser());
        await dbContext.SaveChangesAsync();

        var configuration = CreateJwtConfiguration();
        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "Password123!"
        });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateAdminUser());
        await dbContext.SaveChangesAsync();

        var configuration = CreateJwtConfiguration();
        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "WrongPassword!"
        });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("admin", "")]
    [InlineData(null, "Password123!")]
    [InlineData("admin", null)]
    public async Task LoginAsync_WhenUsernameOrPasswordIsMissing_ThrowsArgumentException(string? username, string? password)
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var configuration = CreateJwtConfiguration();
        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Username = username ?? string.Empty,
            Password = password ?? string.Empty
        });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task LoginAsync_WhenUserAccountIsInactive_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var inactiveUser = CreateAdminUser();
        inactiveUser.IsActive = false;
        dbContext.Users.Add(inactiveUser);
        await dbContext.SaveChangesAsync();

        var configuration = CreateJwtConfiguration();
        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "Password123!"
        });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task LoginAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var configuration = CreateJwtConfiguration();
        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task LoginAsync_WhenJwtKeyIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateAdminUser());
        await dbContext.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "CapitecTransactionAPI"
            })
            .Build();

        IAuthService service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "Password123!"
        });

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IConfiguration CreateJwtConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsASecretKeyForTesting123456",
                ["Jwt:Issuer"] = "CapitecTransactionAPI"
            })
            .Build();
    }

    private static User CreateAdminUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = "admin@capitec.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Admin,
            IsActive = true
        };
    }
}
