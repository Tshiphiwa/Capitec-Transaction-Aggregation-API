using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface IIngestionService
{
    Task<IngestionService.IngestionResultDto> IngestAllSourcesAsync();
    Task<int> ImportTransactionsAsync(IEnumerable<Transaction> transactions, Guid sourceId);
    Task<IngestionService.SourceIngestionResult> IngestSourceAsync(TransactionSource source);
}
