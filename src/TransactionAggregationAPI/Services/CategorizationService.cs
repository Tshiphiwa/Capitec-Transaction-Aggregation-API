using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class CategorizationService : ICategorizationService
{
    public string CategorizeTransaction(Transaction transaction)
    {
        return Categorize(transaction.MccCode, transaction.Description).Category;
    }
}