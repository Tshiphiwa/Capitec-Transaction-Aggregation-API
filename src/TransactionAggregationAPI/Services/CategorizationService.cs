using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services;

public class CategorizationService
{
    // Only use this keyword map if MCC code is not available or does not yield a category
    private static readonly List<(string[] Keywords, string Category)> KeyWordMap = new()
    {
        (new[] { "GROCERY", "SUPERMARKET", "FOOD" }, "Groceries"),
        (new[] { "RESTAURANT", "CAFE", "DINING" }, "Dining"),
        (new[] { "VODACOM", "MTN", "CELL C" }, "Utilities"),
        (new[] { "FUEL", "GAS STATION", "PETROL" }, "Fuel"),
        (new[] { "ENTERTAINMENT", "MOVIE", "THEATER" }, "Entertainment"),
        (new[] { "TRAVEL", "AIRLINE", "HOTEL" }, "Travel"),
        (new[] { "HEALTHCARE", "PHARMACY", "MEDICAL" }, "Healthcare"),
        (new[] { "ELECTRONICS", "TECHNOLOGY", "GADGET" }, "Electronics"),
        (new[] { "CLOTHING", "APPAREL", "FASHION" }, "Clothing"),
        (new[] { "SPORTS", "FITNESS", "GYM" }, "Sports & Fitness"),
        (new[] { "TUITION", "SCHOOL FEES", "UNIVERSITY" }, "Education"),
        (new[] { "SALARY", "PAYROLL", "WAGES", "BONUS", "STIPEND" }, "Income"),
        (new[] { "EDUCATION", "SCHOOL", "UNIVERSITY" }, "Cash"),
        (new[] { "TRANSFER", "SEND MONEY", "PAYMENT TO" }, "Transfers")
    };

    public (string Category, CategorySource Source) Categorize(string? mccCode, string? description)
    {
        var mccCategory = MccCategoryMap.GetCategory(mccCode);

        if (!string.IsNullOrWhiteSpace(mccCategory))
        {
            return (mccCategory, CategorySource.MccCode);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var upper = description.ToUpperInvariant();
            foreach (var (keywords, category) in KeyWordMap)
            {
                if (keywords.Any(k => upper.Contains(k)))
                {
                    return (category, CategorySource.Keyword);
                }
            }
        }

        return ("Uncategorised", CategorySource.Uncategorised);
    }
}