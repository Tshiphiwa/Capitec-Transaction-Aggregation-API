using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace TransactionAggregationAPI.Tests;

public class CategorizationServiceTests
{
    [Fact]
    public void CategorizeTransaction_WhenMccCodeMatchesKnownCategory_ReturnsGroceries()
    {
        // Arrange
        ICategorizationService service = new CategorizationService();
        var transaction = new Transaction
        {
            MccCode = "5411",
            Description = "Groceries"
        };

        // Act
        var category = service.CategorizeTransaction(transaction);

        // Assert
        Assert.Equal("Groceries", category);
    }

    [Fact]
    public void CategorizeTransaction_WhenDescriptionMatchesKnownKeyword_ReturnsUtilities()
    {
        // Arrange
        ICategorizationService service = new CategorizationService();
        var transaction = new Transaction
        {
            MccCode = "9999",
            Description = "Vodacom recharge"
        };

        // Act
        var category = service.CategorizeTransaction(transaction);

        // Assert
        Assert.Equal("Utilities", category);
    }
}
