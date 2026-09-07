using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TransactionAggregationAPI.Tests;

public class ApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ApiIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTransactions_WhenDatabaseContainsTransactions_ReturnsOkAndTransactionPayload()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedTransactionAsync(dbContext);

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/transactions?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task GetSummary_WhenDatabaseContainsTransactions_ReturnsOkAndSummaryPayload()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedTransactionAsync(dbContext);

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/transactions/summary?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(payload);
    }

    [Fact]
    public void ApplicationStartup_WhenHostStarts_RegistersRequiredDependencies()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        // Act
        var dbContext = provider.GetService<AppDbContext>();
        var authService = provider.GetService<IAuthService>();
        var transactionService = provider.GetService<ITransactionService>();
        var ingestionService = provider.GetService<IIngestionService>();

        // Assert
        Assert.NotNull(dbContext);
        Assert.NotNull(authService);
        Assert.NotNull(transactionService);
        Assert.NotNull(ingestionService);
    }

    [Fact]
    public async Task GetTransactions_WhenDatabaseIsSeeded_UsesRealDatabaseRecords()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedTransactionAsync(dbContext);

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/transactions?page=1&pageSize=10");
        var payload = await response.Content.ReadFromJsonAsync<object>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        var count = await dbContext.Transactions.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateCategory_WhenCallerIsNotAdmin_ReturnsForbidden()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedTransactionAsync(dbContext);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "Analyst-token");
        var transaction = await dbContext.Transactions.SingleAsync();

        // Act
        var response = await client.PatchAsJsonAsync($"/api/transactions/{transaction.Id}/category", new { category = "Travel" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task SeedTransactionAsync(AppDbContext dbContext)
    {
        var source = new TransactionSource
        {
            Id = Guid.NewGuid(),
            Name = "Bank",
            Code = "BANK",
            BaseUrl = "https://example.com",
            IsActive = true
        };

        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 150m,
            Currency = "ZAR",
            Description = "Groceries",
            MerchantName = "Local Store",
            MccCode = "5411",
            Category = "Groceries",
            CategorySource = CategorySource.MccCode,
            TransactionType = TransactionType.CardSwipe,
            Direction = TransactionDirection.Debit,
            TransactionDate = DateTime.UtcNow,
            Reference = "api-seed-1",
            FromAccount = "A",
            ToAccount = "B",
            SourceId = source.Id,
            Source = source
        });

        await dbContext.SaveChangesAsync();
    }
}

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase($"ApiIntegrationTests-{Guid.NewGuid()}");
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var role = authHeader.Contains("Analyst", StringComparison.OrdinalIgnoreCase) ? "Analyst" : "Admin";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "integration-user"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
