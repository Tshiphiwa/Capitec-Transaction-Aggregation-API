using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface IIngestionService
{
    Task<IngestionResultDto> IngestAllSourcesAsync();
    Task<int> ImportTransactionsAsync(IEnumerable<Transaction> transactions, Guid sourceId);
    Task<SourceIngestionResult> IngestSourceAsync(TransactionSource source);
}
