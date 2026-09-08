using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace TransactionAggregationAPI.Tests;

public class TransactionServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITransactionMapper> _mapperMock;
    private readonly ITransactionService _sut;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TransactionServiceTests-{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _mapperMock = new Mock<ITransactionMapper>();
        _mapperMock
            .Setup(m => m.MapToDto(It.IsAny<Transaction>()))
            .Returns<Transaction>(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Category = t.Category,
                CategorySource = t.CategorySource.ToString(),
                Direction = t.Direction.ToString(),
                Description = t.Description,
                Reference = t.Reference,
                SourceCode = t.Source?.Code ?? string.Empty,
                SourceName = t.Source?.Name ?? string.Empty
            });

        _sut = new TransactionService(_dbContext, _mapperMock.Object, NullLogger<TransactionService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // ── GetTransactionsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionsAsync_WhenFilterHasValidPaging_ReturnsPagedTransactions()
    {
        var source = SeedSource();
        SeedTransactions(source, ("ref-1", 100m, "Groceries"), ("ref-2", 250m, "Salary"), ("ref-3", 75m, "Transport"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { Page = 1, PageSize = 2 });

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenPageSizeIsZero_ReturnsAllTransactions()
    {
        var source = SeedSource();
        SeedTransactions(source, ("ref-1", 100m, "Groceries"), ("ref-2", 250m, "Salary"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { Page = 1, PageSize = 0 });

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenCategoryFilterApplied_ReturnsOnlyMatchingTransactions()
    {
        var source = SeedSource();
        SeedTransactions(source, ("ref-1", 100m, "Groceries"), ("ref-2", 250m, "Dining"), ("ref-3", 75m, "Groceries"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { Category = "Groceries" });

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(t => t.Category.Should().Be("Groceries"));
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenAmountRangeFilterApplied_ReturnsOnlyTransactionsInRange()
    {
        var source = SeedSource();
        SeedTransactions(source, ("ref-1", 50m, "Groceries"), ("ref-2", 200m, "Dining"), ("ref-3", 500m, "Travel"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { MinAmount = 100m, MaxAmount = 300m });

        result.TotalCount.Should().Be(1);
        result.Items.Single().Amount.Should().Be(200m);
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenSearchTermMatchesDescription_ReturnsMatchingTransactions()
    {
        var source = SeedSource();
        SeedTransactions(source, ("ref-1", 100m, "Grocery Store"), ("ref-2", 200m, "Airline Ticket"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { Search = "grocery" });

        result.TotalCount.Should().Be(1);
        result.Items.Single().Description.Should().Contain("Grocery");
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenDirectionFilterApplied_ReturnsOnlyMatchingDirection()
    {
        var source = SeedSource();
        var debit = BuildTransaction(source, "ref-1", 100m, "Groceries", TransactionDirection.Debit);
        var credit = BuildTransaction(source, "ref-2", 500m, "Salary", TransactionDirection.Credit);
        _dbContext.Transactions.AddRange(debit, credit);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionsAsync(new TransactionFilterDto { Direction = "Credit" });

        result.TotalCount.Should().Be(1);
        result.Items.Single().Direction.Should().Be("Credit");
    }

    // ── GetTransactionByIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionByIdAsync_WhenTransactionExists_ReturnsMappedDto()
    {
        var source = SeedSource();
        var tx = BuildTransaction(source, "ref-1", 100m, "Groceries");
        _dbContext.Transactions.Add(tx);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionByIdAsync(tx.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(tx.Id);
        _mapperMock.Verify(m => m.MapToDto(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WhenTransactionDoesNotExist_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.GetTransactionByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateCategoryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategoryAsync_WhenCalledWithValidCategory_PersistsCategoryAndMarksManual()
    {
        var source = SeedSource();
        var tx = BuildTransaction(source, "ref-1", 100m, "Groceries");
        _dbContext.Transactions.Add(tx);
        await _dbContext.SaveChangesAsync();

        await _sut.UpdateCategoryAsync(tx.Id, "Food");

        var saved = await _dbContext.Transactions.FindAsync(tx.Id);
        saved!.Category.Should().Be("Food");
        saved.CategorySource.Should().Be(CategorySource.Manual);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryIsWhitespace_ThrowsArgumentException()
    {
        var source = SeedSource();
        var tx = BuildTransaction(source, "ref-1", 100m, "Groceries");
        _dbContext.Transactions.Add(tx);
        await _dbContext.SaveChangesAsync();

        var act = async () => await _sut.UpdateCategoryAsync(tx.Id, "   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenTransactionDoesNotExist_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.UpdateCategoryAsync(Guid.NewGuid(), "Food");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetTransactionSummaryAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionSummaryAsync_WhenNoTransactionsExist_ReturnsZeroedSummary()
    {
        var result = await _sut.GetTransactionSummaryAsync(new TransactionFilterDto());

        result.TotalDebits.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.TransactionCount.Should().Be(0);
        result.TopSpendingCategory.Should().Be("None");
    }

    [Fact]
    public async Task GetTransactionSummaryAsync_WhenTransactionsExist_ReturnsTotalsAndTopCategory()
    {
        var source = SeedSource();
        _dbContext.Transactions.AddRange(
            BuildTransaction(source, "ref-1", 300m, "Groceries", TransactionDirection.Debit),
            BuildTransaction(source, "ref-2", 100m, "Dining", TransactionDirection.Debit),
            BuildTransaction(source, "ref-3", 5000m, "Salary", TransactionDirection.Credit));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTransactionSummaryAsync(new TransactionFilterDto());

        result.TotalDebits.Should().Be(400m);
        result.TotalCredits.Should().Be(5000m);
        result.NetAmount.Should().Be(4600m);
        result.TopSpendingCategory.Should().Be("Groceries");
    }

    // ── GetAggregatedTransactionsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetAggregatedTransactionsAsync_WhenNoTransactionsExist_ReturnsEmptyAggregation()
    {
        var result = await _sut.GetAggregatedTransactionsAsync(new TransactionFilterDto());

        result.Categories.Should().BeEmpty();
        result.GrandTotal.Should().Be(0m);
        result.TotalTransactions.Should().Be(0);
    }

    [Fact]
    public async Task GetAggregatedTransactionsAsync_WhenDebitsExist_GroupsByCategoryWithPercentages()
    {
        var source = SeedSource();
        _dbContext.Transactions.AddRange(
            BuildTransaction(source, "ref-1", 200m, "Groceries", TransactionDirection.Debit),
            BuildTransaction(source, "ref-2", 200m, "Groceries", TransactionDirection.Debit),
            BuildTransaction(source, "ref-3", 400m, "Dining", TransactionDirection.Debit));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAggregatedTransactionsAsync(new TransactionFilterDto());

        result.GrandTotal.Should().Be(800m);
        result.TotalTransactions.Should().Be(3);
        var groceries = result.Categories.Single(c => c.Category == "Groceries");
        groceries.PercentageOfTotalSpend.Should().Be(50m);
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private TransactionSource SeedSource(string name = "Bank", string code = "BANK")
    {
        var source = new TransactionSource { Id = Guid.NewGuid(), Name = name, Code = code, BaseUrl = "https://example.com" };
        _dbContext.TransactionSources.Add(source);
        return source;
    }

    private void SeedTransactions(TransactionSource source, params (string Ref, decimal Amount, string Category)[] items)
    {
        foreach (var (r, a, c) in items)
            _dbContext.Transactions.Add(BuildTransaction(source, r, a, c));
    }

    private static Transaction BuildTransaction(
        TransactionSource source,
        string reference,
        decimal amount,
        string category,
        TransactionDirection direction = TransactionDirection.Debit) => new()
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
        Direction = direction,
        TransactionDate = DateTime.UtcNow,
        Reference = reference,
        FromAccount = "A",
        ToAccount = "B",
        SourceId = source.Id,
        Source = source
    };
}
