using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionReferenceChecker : ITransactionReferenceChecker
{
    public Task<bool> ExistsAsync(AppDbContext dbContext, string reference, Guid sourceId)
    {
        return dbContext.Transactions.AnyAsync(t => t.Reference == reference && t.SourceId == sourceId);
    }

    public Task<bool> ExistsAsync(AppDbContext dbContext, Transaction transaction, Guid sourceId)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return ExistsAsync(dbContext, transaction.Reference, sourceId);
    }
}
