using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ICategorizationService
{
    (string Category, CategorySource Source) Categorize(string? mccCode, string? description);
}
