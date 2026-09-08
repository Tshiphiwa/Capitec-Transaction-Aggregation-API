using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TransactionAggregationAPI.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task GetTransactionsAsync_WhenFilterHasValidPaging_ReturnsPagedTransactions()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.AddRange(
            CreateTransaction(source, "ref-1", 100m, "Groceries"),
            CreateTransaction(source, "ref-2", 250m, "Salary"),
            CreateTransaction(source, "ref-3", 75m, "Transport"));
        await dbContext.SaveChangesAsync();

        ITransactionService service = new TransactionService(dbContext);

        // Act
        var result = await service.GetTransactionsAsync(new TransactionFilterDto
        {
            Page = 1,
            PageSize = 2
        });

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenPageSizeIsZero_UsesDefaultPageSize()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.AddRange(
            CreateTransaction(source, "ref-1", 100m, "Groceries"),
            CreateTransaction(source, "ref-2", 250m, "Salary"));
        await dbContext.SaveChangesAsync();

        ITransactionService service = new TransactionService(dbContext);

        // Act
        var result = await service.GetTransactionsAsync(new TransactionFilterDto
        {
            Page = 1,
            PageSize = 0
        });

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenUserIsAdmin_UpdatesCategoryAndMarksItAsManual()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        var transaction = CreateTransaction(source, "ref-1", 100m, "Groceries");

        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        ITransactionService service = new TransactionService(dbContext);

        // Act
        var result = await service.UpdateCategoryAsync(transaction.Id, "Food");

        // Assert
        Assert.Equal("Food", result.Category);
        Assert.Equal("Manual", result.CategorySource);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenUserIsNotAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        var transaction = CreateTransaction(source, "ref-1", 100m, "Groceries");

        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        ITransactionService service = new TransactionService(dbContext);

        // Act
        var act = async () => await service.UpdateCategoryAsync(transaction.Id, "Food", UserRole.Analyst);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WhenTransactionDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        ITransactionService service = new TransactionService(dbContext);

        // Act
        var act = async () => await service.GetTransactionByIdAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryIsBlank_ThrowsArgumentException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        var transaction = CreateTransaction(source, "ref-1", 100m, "Groceries");

        dbContext.TransactionSources.Add(source);
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        ITransactionService service = new TransactionService(dbContext);

        // Act
        var act = async () => await service.UpdateCategoryAsync(transaction.Id, "   ", UserRole.Admin);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetTransactionSummaryAsync_WhenNoTransactionsExist_ReturnsEmptySummary()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        ITransactionService service = new TransactionService(dbContext);

        // Act
        var result = await service.GetTransactionSummaryAsync(new TransactionFilterDto());

        // Assert
        Assert.Equal(0m, result.TotalDebits);
        Assert.Equal(0m, result.TotalCredits);
        Assert.Equal(0, result.TransactionCount);
        Assert.Equal("None", result.TopSpendingCategory);
    }

    [Fact]
    public async Task GetAggregatedTransactionsAsync_WhenNoTransactionsExist_ReturnsEmptyAggregation()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        ITransactionService service = new TransactionService(dbContext);

        // Act
        var result = await service.GetAggregatedTransactionsAsync(new TransactionFilterDto());

        // Assert
        Assert.Empty(result.Categories);
        Assert.Equal(0m, result.GrandTotal);
        Assert.Equal(0, result.TotalTransactions);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static TransactionSource CreateSource(string name = "Bank", string code = "BANK")
    {
        return new TransactionSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            BaseUrl = "https://example.com"
        };
    }

    private static Transaction CreateTransaction(TransactionSource source, string reference, decimal amount, string category)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = "ZAR",
            Description = category,
            MerchantName = "Store",
            MccCode = "5411",
            Category = category,
            CategorySource = CategorySource.MccCode,
            TransactionType = TransactionType.CardSwipe,
            Direction = TransactionDirection.Debit,
            TransactionDate = DateTime.UtcNow,
            Reference = reference,
            FromAccount = "A",
            ToAccount = "B",
            SourceId = source.Id,
            Source = source
        };
    }
}
