using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionSourceClient
{
    Task<IReadOnlyList<RawTransactionDto>> GetTransactionsAsync(TransactionSource source, CancellationToken cancellationToken = default);
}
