namespace Capitec_Transaction_Aggregation_API.Infrastructure;

public class MccCategoryMap
{
    private static readonly Dictionary<string, string> Map = new()
    {
        { "5411", "Groceries" }, { "5412", "Groceries" }, { "5413", "Groceries" },
        { "5414", "Groceries" }, { "5415", "Groceries" }, { "5416", "Groceries" }, { "5417", "Groceries" },

        { "5812", "Dining" }, { "5813", "Dining" }, { "5814", "Dining" }, { "5815", "Dining" },

        { "7800", "Entertainment" }, { "7801", "Entertainment" }, { "7802", "Entertainment" },
        { "7995", "Entertainment" }, { "7929", "Entertainment" }, { "7933", "Entertainment" },

        { "8011", "Healthcare" }, { "8021", "Healthcare" }, { "8041", "Healthcare" },
        { "8051", "Healthcare" }, { "8061", "Healthcare" },

        { "5311", "Shopping" }, { "5399", "Shopping" },

        { "4900", "Utilities" }, { "4901", "Utilities" }, { "4902", "Utilities" },

        { "8211", "Education" }, { "8220", "Education" },

        { "5039", "Home" }, { "5072", "Home" },

        { "3111", "Travel" }, { "3120", "Travel" }, { "3318", "Travel" },

        { "4730", "Fuel" },

        { "5812", "Takeaways" }, { "5813", "Takeaways" },

        { "5420", "Alcohol" },

        { "5812", "Restaurants" }, { "5813", "Restaurants" }, { "5814", "Restaurants" },

        { "4900", "Electricity" },

        { "5611", "Clothing & Shoes" }, { "5621", "Clothing & Shoes" }, { "5631", "Clothing & Shoes" },
        { "5661", "Clothing & Shoes" },
        
        // Will use these for the ATM withdrawals
        {"6010", "Cash"}, {"6011", "Cash"}, {"6012", "Cash"},
    };

    public static string? GetCategory(string? mccCode)
    {
        if (string.IsNullOrWhiteSpace(mccCode))
            return null;
        return Map.TryGetValue(mccCode.Trim(), out var category) ? category : null;
    }
}