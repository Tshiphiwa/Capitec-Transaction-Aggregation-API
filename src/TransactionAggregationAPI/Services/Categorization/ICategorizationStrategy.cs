using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Categorization;

// each implementation is one categorization approach, new ones can be added without touching existing ones
public interface ICategorizationStrategy
{
    (string Category, CategorySource Source)? TryCategorize(string? mccCode, string? description);
}