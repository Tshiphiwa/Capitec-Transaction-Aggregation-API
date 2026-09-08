using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using FluentAssertions;

namespace TransactionAggregationAPI.Tests;

public class TransactionMapperTests
{
    private readonly TransactionMapper _sut = new();

    // ── MapToDto ──────────────────────────────────────────────────────────────

    [Fact]
    public void MapToDto_WhenTransactionHasAllFields_MapsAllPropertiesCorrectly()
    {
        var source = new TransactionSource { Id = Guid.NewGuid(), Name = "Card System", Code = "CARD", BaseUrl = "https://example.com" };
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = 250.50m,
            Currency = "ZAR",
            Description = "Grocery Store",
            MerchantName = "Pick n Pay",
            MccCode = "5411",
            Category = "Groceries",
            CategorySource = CategorySource.MccCode,
            TransactionType = TransactionType.CardSwipe,
            Direction = TransactionDirection.Debit,
            TransactionDate = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            Reference = "ref-001",
            FromAccount = "ACC-A",
            ToAccount = "ACC-B",
            Source = source,
            CreatedDate = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var dto = _sut.MapToDto(tx);

        dto.Id.Should().Be(tx.Id);
        dto.Amount.Should().Be(250.50m);
        dto.Currency.Should().Be("ZAR");
        dto.Description.Should().Be("Grocery Store");
        dto.MerchantName.Should().Be("Pick n Pay");
        dto.MccCode.Should().Be("5411");
        dto.Category.Should().Be("Groceries");
        dto.CategorySource.Should().Be("MccCode");
        dto.TransactionType.Should().Be("CardSwipe");
        dto.Direction.Should().Be("Debit");
        dto.SourceName.Should().Be("Card System");
        dto.SourceCode.Should().Be("CARD");
    }

    [Fact]
    public void MapToDto_WhenMerchantNameIsNull_ReturnsEmptyString()
    {
        var tx = BuildMinimalTransaction();
        tx.MerchantName = null;

        var dto = _sut.MapToDto(tx);

        dto.MerchantName.Should().BeEmpty();
    }

    [Fact]
    public void MapToDto_WhenSourceIsNull_ReturnsEmptySourceFields()
    {
        var tx = BuildMinimalTransaction();
        tx.Source = null!;

        var dto = _sut.MapToDto(tx);

        dto.SourceName.Should().BeEmpty();
        dto.SourceCode.Should().BeEmpty();
    }

    // ── MapTransactionType ────────────────────────────────────────────────────

    [Theory]
    [InlineData("salary_credit", TransactionType.SalaryCredit)]
    [InlineData("eft_transfer", TransactionType.EftTransfer)]
    [InlineData("eft_credit", TransactionType.EftTransfer)]
    [InlineData("eft_debit", TransactionType.EftTransfer)]
    public void MapTransactionType_WhenKnownTypeProvided_ReturnsMappedEnum(string raw, TransactionType expected)
    {
        _sut.MapTransactionType(raw).Should().Be(expected);
    }

    [Fact]
    public void MapTransactionType_WhenUnknownTypeProvided_DefaultsToEftTransfer()
    {
        _sut.MapTransactionType("UNKNOWN_TYPE").Should().Be(TransactionType.EftTransfer);
    }

    // ── MapTransactionDirection ───────────────────────────────────────────────

    [Fact]
    public void MapTransactionDirection_WhenCreditProvided_ReturnsCredit()
    {
        _sut.MapTransactionDirection("credit").Should().Be(TransactionDirection.Credit);
    }

    [Fact]
    public void MapTransactionDirection_WhenDebitOrUnknownProvided_ReturnsDebit()
    {
        _sut.MapTransactionDirection("debit").Should().Be(TransactionDirection.Debit);
        _sut.MapTransactionDirection("anything").Should().Be(TransactionDirection.Debit);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Transaction BuildMinimalTransaction() => new()
    {
        Id = Guid.NewGuid(),
        Amount = 100m,
        Currency = "ZAR",
        Description = "Test",
        Category = "Groceries",
        CategorySource = CategorySource.MccCode,
        TransactionType = TransactionType.CardSwipe,
        Direction = TransactionDirection.Debit,
        TransactionDate = DateTime.UtcNow,
        Reference = "ref-min",
        Source = new TransactionSource { Name = "Bank", Code = "BANK", BaseUrl = "https://example.com" }
    };
}
