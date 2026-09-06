using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace TransactionAggregationAPI.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsToken_ForValidUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = "admin@capitec.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Admin,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsASecretKeyForTesting123456",
                ["Jwt:Issuer"] = "CapitecTransactionAPI"
            })
            .Build();

        var service = new AuthService(dbContext, configuration, NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "Password123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
    }
}

public class CategorizationServiceTests
{
    [Fact]
    public void CategorizeTransaction_UsesMccCode_WhenAvailable()
    {
        var service = new CategorizationService();

        var category = service.CategorizeTransaction(new Transaction
        {
            MccCode = "5411",
            Description = "Groceries"
        });

        Assert.Equal("Groceries", category);
    }
}

public class TransactionServiceTests
{
    [Fact]
    public async Task GetTransactionsAsync_ReturnsPagedTransactions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);
        var source = new TransactionSource
        {
            Id = Guid.NewGuid(),
            Name = "Bank",
            Code = "BANK",
            BaseUrl = "https://example.com"
        };

        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = 100m,
                Currency = "ZAR",
                Description = "Groceries",
                MerchantName = "Store",
                MccCode = "5411",
                Category = "Groceries",
                CategorySource = CategorySource.MccCode,
                TransactionType = TransactionType.CardSwipe,
                Direction = TransactionDirection.Debit,
                TransactionDate = DateTime.UtcNow,
                Reference = "ref-1",
                FromAccount = "A",
                ToAccount = "B",
                SourceId = source.Id,
                Source = source
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = 250m,
                Currency = "ZAR",
                Description = "Salary",
                MerchantName = "Employer",
                MccCode = null,
                Category = "Salary",
                CategorySource = CategorySource.Keyword,
                TransactionType = TransactionType.SalaryCredit,
                Direction = TransactionDirection.Credit,
                TransactionDate = DateTime.UtcNow,
                Reference = "ref-2",
                FromAccount = "A",
                ToAccount = "B",
                SourceId = source.Id,
                Source = source
            });

        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext);
        var result = await service.GetTransactionsAsync(new TransactionFilterDto
        {
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }
}

public class IngestionServiceTests
{
    [Fact]
    public async Task ImportTransactionsAsync_PersistsTransactions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AppDbContext(options);
        var source = new TransactionSource
        {
            Id = Guid.NewGuid(),
            Name = "Bank",
            Code = "BANK",
            BaseUrl = "https://example.com"
        };
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = new IngestionService(dbContext, new CategorizationService());
        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = 99.99m,
                Currency = "ZAR",
                Description = "Groceries",
                MerchantName = "Store",
                MccCode = "5411",
                TransactionType = TransactionType.CardSwipe,
                Direction = TransactionDirection.Debit,
                TransactionDate = DateTime.UtcNow,
                Reference = "seed-1",
                FromAccount = "A",
                ToAccount = "B",
                SourceId = source.Id,
                Source = source
            }
        };

        var imported = await service.ImportTransactionsAsync(transactions, source.Id);

        Assert.Equal(1, imported);
        Assert.Equal("Groceries", dbContext.Transactions.Single().Category);
    }
}