using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

        var jwtTokenService = CreateJwtTokenService();
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@capitec.com",
            Password = "Password123!"
        });

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task SeedAsync_WhenNoUsersExist_CreatesAdminUserWithExpectedPassword()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MockSources:Card:BaseUrl"] = "http://localhost:5000/api/mock-sources/card",
                ["MockSources:Eft:BaseUrl"] = "http://localhost:5000/api/mock-sources/eft",
                ["MockSources:Wallet:BaseUrl"] = "http://localhost:5000/api/mock-sources/wallet"
            })
            .Build();
        var seeder = new DatabaseSeeder(dbContext, NullLogger<DatabaseSeeder>.Instance, configuration);

        // Act
        await seeder.SeedAsync();
        var adminUser = await dbContext.Users.SingleAsync();

        // Assert
        Assert.Equal("admin", adminUser.UserName);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", adminUser.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateAdminUser());
        await dbContext.SaveChangesAsync();

        var jwtTokenService = CreateJwtTokenService();
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@capitec.com",
            Password = "WrongPassword!"
        });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("admin@capitec.com", "")]
    [InlineData(null, "Password123!")]
    [InlineData("admin@capitec.com", null)]
    public async Task LoginAsync_WhenUsernameOrPasswordIsMissing_ThrowsArgumentException(string? email, string? password)
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var jwtTokenService = CreateJwtTokenService();
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Email = email ?? string.Empty,
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

        var jwtTokenService = CreateJwtTokenService();
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@capitec.com",
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
        var jwtTokenService = CreateJwtTokenService();
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task LoginAsync_WhenJwtKeyIsMissing_ThrowsArgumentException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateAdminUser());
        await dbContext.SaveChangesAsync();

        var jwtTokenService = new JwtTokenService(Options.Create(new JwtOptions { Issuer = "CapitecTransactionAPI" }));
        IAuthService service = new AuthService(dbContext, jwtTokenService, NullLogger<AuthService>.Instance);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@capitec.com",
            Password = "Password123!"
        });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IJwtTokenService CreateJwtTokenService()
    {
        return new JwtTokenService(Options.Create(new JwtOptions
        {
            Key = "ThisIsASecretKeyForTesting123456",
            Issuer = "CapitecTransactionAPI"
        }));
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
