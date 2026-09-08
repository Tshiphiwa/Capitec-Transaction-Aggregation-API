using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace TransactionAggregationAPI.Tests;

public class JwtTokenServiceTests
{
    private static readonly User AdminUser = new()
    {
        Id = Guid.NewGuid(),
        UserName = "admin",
        Email = "admin@test.com",
        Role = UserRole.Admin,
        PasswordHash = "hash",
        IsActive = true
    };

    // ── GenerateToken ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_WhenValidUserProvided_ReturnsNonEmptyToken()
    {
        var sut = CreateService();

        var (token, _) = sut.GenerateToken(AdminUser);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_WhenValidUserProvided_TokenContainsExpectedClaims()
    {
        var sut = CreateService();

        var (token, _) = sut.GenerateToken(AdminUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == AdminUser.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == AdminUser.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateToken_WhenValidUserProvided_ExpiresAtIsInTheFuture()
    {
        var sut = CreateService(expiryHours: 8);

        var (_, expiresAt) = sut.GenerateToken(AdminUser);

        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateToken_WhenCalledTwice_ProducesUniqueJtiClaims()
    {
        var sut = CreateService();

        var (token1, _) = sut.GenerateToken(AdminUser);
        var (token2, _) = sut.GenerateToken(AdminUser);

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateToken_WhenKeyIsMissing_ThrowsArgumentException()
    {
        var sut = new JwtTokenService(Options.Create(new JwtOptions { Key = string.Empty, Issuer = "Test" }));

        var act = () => sut.GenerateToken(AdminUser);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JwtTokenService CreateService(int expiryHours = 8) =>
        new(Options.Create(new JwtOptions
        {
            Key = "ThisIsASecretKeyForTestingPurposes!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryHours = expiryHours
        }));
}
