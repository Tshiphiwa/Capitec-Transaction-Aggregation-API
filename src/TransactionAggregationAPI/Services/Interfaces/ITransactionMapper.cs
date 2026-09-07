using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionMapper
{
    Transaction MapToTransaction(RawTransactionDto raw, TransactionSource source, ICategorizationService categorizationService, ILogger logger);
    void ApplyDefaultCategorization(Transaction transaction, ICategorizationService categorizationService);
    TransactionType MapTransactionType(string rawType, ILogger logger);
    TransactionDirection MapTransactionDirection(string rawDirection, ILogger logger);
}
