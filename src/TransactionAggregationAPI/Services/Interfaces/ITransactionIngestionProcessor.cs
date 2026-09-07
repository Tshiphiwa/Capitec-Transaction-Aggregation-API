using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionIngestionProcessor
{
    Task<SourceIngestionResult> ProcessAsync(TransactionSource source, CancellationToken cancellationToken = default);
}
