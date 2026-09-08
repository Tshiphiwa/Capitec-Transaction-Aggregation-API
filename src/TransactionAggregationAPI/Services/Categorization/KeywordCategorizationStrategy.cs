using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Categorization;

public class KeywordCategorizationStrategy : ICategorizationStrategy 
{
   // Only use this keyword map if MCC code is not available or does not yield a category
    private static readonly IReadOnlyList<(string[] Keywords, string Category)> KeyWordMap =
    [
        ([ "GROCERY", "SUPERMARKET", "FOOD" ], "Groceries"),
        ([ "RESTAURANT", "CAFE", "DINING" ], "Dining"),
        ([ "VODACOM", "MTN", "CELL C" ], "Utilities"),
        ([ "FUEL", "GAS STATION", "PETROL" ], "Fuel"),
        ([ "ENTERTAINMENT", "MOVIE", "THEATER" ], "Entertainment"),
        ([ "TRAVEL", "AIRLINE", "HOTEL" ], "Travel"),
        ([ "HEALTHCARE", "PHARMACY", "MEDICAL" ], "Healthcare"),
        ([ "ELECTRONICS", "TECHNOLOGY", "GADGET" ], "Electronics"),
        ([ "CLOTHING", "APPAREL", "FASHION" ], "Clothing"),
        ([ "SPORTS", "FITNESS", "GYM" ], "Sports & Fitness"),
        ([ "TUITION", "SCHOOL FEES", "UNIVERSITY" ], "Education"),
        ([ "SALARY", "PAYROLL", "WAGES", "BONUS", "STIPEND" ], "Income"),
        ([ "EDUCATION", "SCHOOL", "UNIVERSITY" ], "Cash"),
        ([ "TRANSFER", "SEND MONEY", "PAYMENT TO" ], "Transfers"),
    ];

    public (string Category, CategorySource Source)? TryCategorize(string? mccCode, string? description)
    {

        if (string.IsNullOrWhiteSpace(description))
            return null;

        var upper = description.ToUpperInvariant();    

        
        foreach (var (keywords, category) in KeyWordMap)
        {
            if (keywords.Any(k => upper.Contains(k)))
                return (category, CategorySource.Keyword);
        }

        return null;
    }
}