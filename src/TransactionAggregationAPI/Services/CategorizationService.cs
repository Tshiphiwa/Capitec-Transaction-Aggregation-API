using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Categorization;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class CategorizationService : ICategorizationService
{
    private readonly IReadOnlyList<ICategorizationStrategy> _strategies;

    public CategorizationService(IEnumerable<ICategorizationStrategy> strategies)
    {
        _strategies = strategies.ToList();
    }

    public (string Category, CategorySource Source) Categorize(string? mccCode, string? description)
    {
        foreach (var strategy in _strategies)
        {
            var result = strategy.TryCategorize(mccCode, description);
            if (result.HasValue)
                return result.Value;
        }

        return ("Uncategorized", CategorySource.Uncategorised);
    }
}