using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace TransactionAggregationAPI.Tests;

public class SourceServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly SourceService _sut;

    public SourceServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"SourceServiceTests-{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);
        _sut = new SourceService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // ── GetSourcesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSourcesAsync_WhenNoSourcesExist_ReturnsEmptyList()
    {
        var result = await _sut.GetSourcesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSourcesAsync_WhenSourcesExist_ReturnsAllSources()
    {
        _dbContext.TransactionSources.AddRange(
            BuildSource("Card System", "CARD"),
            BuildSource("EFT System", "EFT"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetSourcesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Code == "CARD");
        result.Should().Contain(s => s.Code == "EFT");
    }

    [Fact]
    public async Task GetSourcesAsync_WhenSourceHasTransactions_ReturnsCorrectTransactionCount()
    {
        var source = BuildSource("Card System", "CARD");
        _dbContext.TransactionSources.Add(source);
        _dbContext.Transactions.AddRange(
            BuildTransaction(source, "ref-1"),
            BuildTransaction(source, "ref-2"),
            BuildTransaction(source, "ref-3"));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetSourcesAsync();

        result.Single().TransactionCount.Should().Be(3);
    }

    [Fact]
    public async Task GetSourcesAsync_WhenSourceIsInactive_StillIncludesItInResults()
    {
        _dbContext.TransactionSources.Add(BuildSource("Old System", "OLD", isActive: false));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetSourcesAsync();

        result.Should().HaveCount(1);
        result.Single().IsActive.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TransactionSource BuildSource(string name, string code, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Code = code,
        BaseUrl = "https://example.com",
        IsActive = isActive
    };

    private static Transaction BuildTransaction(TransactionSource source, string reference) => new()
    {
        Id = Guid.NewGuid(),
        Amount = 100m,
        Currency = "ZAR",
        Description = "Test",
        MerchantName = "Store",
        Category = "Groceries",
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
