using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Categorization;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using FluentAssertions;

namespace TransactionAggregationAPI.Tests;

public class CategorizationServiceTests
{
    private readonly ICategorizationService _sut = new CategorizationService(new ICategorizationStrategy[]
    {
        new MccCategorizationStrategy(),
        new KeywordCategorizationStrategy()
    });

    // ── MCC Strategy ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("5411", "Groceries")]
    [InlineData("5812", "Dining")]
    [InlineData("8011", "Healthcare")]
    [InlineData("4730", "Fuel")]
    [InlineData("8211", "Education")]
    public void Categorize_WhenMccCodeMatchesKnownCode_ReturnsExpectedCategory(string mccCode, string expectedCategory)
    {
        var (category, source) = _sut.Categorize(mccCode, "irrelevant description");

        category.Should().Be(expectedCategory);
        source.Should().Be(CategorySource.MccCode);
    }

    [Fact]
    public void Categorize_WhenMccCodeIsUnknown_FallsBackToKeywordStrategy()
    {
        var (category, source) = _sut.Categorize("9999", "Vodacom recharge");

        category.Should().Be("Utilities");
        source.Should().Be(CategorySource.Keyword);
    }

    [Fact]
    public void Categorize_WhenMccCodeIsNull_FallsBackToKeywordStrategy()
    {
        var (category, source) = _sut.Categorize(null, "SALARY payment");

        category.Should().Be("Income");
        source.Should().Be(CategorySource.Keyword);
    }

    [Fact]
    public void Categorize_WhenMccCodeIsWhitespace_FallsBackToKeywordStrategy()
    {
        var (category, source) = _sut.Categorize("   ", "SALARY payment");

        category.Should().Be("Income");
        source.Should().Be(CategorySource.Keyword);
    }

    // ── Keyword Strategy ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("salary deposit", "Income")]
    [InlineData("SALARY CREDIT", "Income")]
    [InlineData("Salary Payment", "Income")]
    public void Categorize_WhenDescriptionContainsSalaryKeyword_ReturnsIncomeRegardlessOfCase(string description, string expectedCategory)
    {
        var (category, source) = _sut.Categorize("9999", description);

        category.Should().Be(expectedCategory);
        source.Should().Be(CategorySource.Keyword);
    }

    [Theory]
    [InlineData("Vodacom recharge", "Utilities")]
    [InlineData("MTN airtime", "Utilities")]
    [InlineData("CELL C data bundle", "Utilities")]
    public void Categorize_WhenDescriptionContainsTelecomKeyword_ReturnsUtilities(string description, string expectedCategory)
    {
        var (category, source) = _sut.Categorize(null, description);

        category.Should().Be(expectedCategory);
        source.Should().Be(CategorySource.Keyword);
    }

    // ── Uncategorised Fallback ────────────────────────────────────────────────

    [Fact]
    public void Categorize_WhenNeitherMccNorKeywordMatches_ReturnsUncategorised()
    {
        var (category, source) = _sut.Categorize("9999", "XYZ unknown merchant 12345");

        category.Should().Be("Uncategorized");
        source.Should().Be(CategorySource.Uncategorised);
    }

    [Fact]
    public void Categorize_WhenBothMccAndDescriptionAreNull_ReturnsUncategorised()
    {
        var (category, source) = _sut.Categorize(null, null);

        category.Should().Be("Uncategorized");
        source.Should().Be(CategorySource.Uncategorised);
    }

    [Fact]
    public void Categorize_WhenDescriptionIsEmpty_ReturnsUncategorised()
    {
        var (category, source) = _sut.Categorize("9999", string.Empty);

        category.Should().Be("Uncategorized");
        source.Should().Be(CategorySource.Uncategorised);
    }

    // ── MCC takes priority over keyword ──────────────────────────────────────

    [Fact]
    public void Categorize_WhenMccMatchesAndDescriptionAlsoMatches_MccTakesPriority()
    {
        // MCC 5411 = Groceries, description would match Dining keyword
        var (category, source) = _sut.Categorize("5411", "restaurant dining");

        category.Should().Be("Groceries");
        source.Should().Be(CategorySource.MccCode);
    }
}
