using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionReferenceChecker
{
    Task<bool> ExistsAsync(AppDbContext dbContext, string reference, Guid sourceId);
    Task<bool> ExistsAsync(AppDbContext dbContext, Transaction transaction, Guid sourceId);
}
