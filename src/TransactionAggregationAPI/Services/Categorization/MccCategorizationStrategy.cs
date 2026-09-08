using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Infrastructure;

namespace Capitec_Transaction_Aggregation_API.Services.Categorization;

public class MccCategorizationStrategy : ICategorizationStrategy
{
    public (string category, CategorySource Source)? TryCategorize(string? mccCode, string? description)
    {
        var category = MccCategoryMap.GetCategory(mccCode);
        return category != null ? (category, CategorySource.MccCode) : null;
    }
}