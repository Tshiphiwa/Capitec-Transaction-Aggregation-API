using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Capitec_Transaction_Aggregation_API.Services;

public class IngestionService : IIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICategorizationService _categorizationService;
    private readonly ITransactionIngestionProcessor _ingestionProcessor;
    private readonly ITransactionReferenceChecker _referenceChecker;
    private readonly ITransactionMapper _transactionMapper;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        AppDbContext dbContext,
        ICategorizationService categorizationService,
        ITransactionIngestionProcessor ingestionProcessor,
        ITransactionReferenceChecker? referenceChecker = null,
        ITransactionMapper? transactionMapper = null,
        ILogger<IngestionService>? logger = null)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _ingestionProcessor = ingestionProcessor;
        _referenceChecker = referenceChecker ?? new TransactionReferenceChecker();
        _transactionMapper = transactionMapper ?? new TransactionMapper();
        _logger = logger ?? NullLogger<IngestionService>.Instance;
    }

    public async Task<IngestionResultDto> IngestAllSourcesAsync()
    {
        var sources = await _dbContext.TransactionSources
            .Where(s => s.IsActive)
            .ToListAsync();

        var result = new IngestionResultDto();

        foreach (var source in sources)
        {
            try
            {
                var sourceResult = await IngestSourceAsync(source);
                result.SourceResults.Add(sourceResult);
                result.TotalIngested += sourceResult.IngestedCount;
                result.TotalSkipped += sourceResult.SkippedCount;
                source.LastUpdatedDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting source {SourceCode}", source.Code);
                result.SourceResults.Add(new SourceIngestionResult
                {
                    SourceCode = source.Code,
                    SourceName = source.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Ingestion completed. Total ingested: {TotalIngested}, Total skipped: {TotalSkipped}",
            result.TotalIngested,
            result.TotalSkipped);

        return result;
    }

    public async Task<int> ImportTransactionsAsync(IEnumerable<Transaction> transactions, Guid sourceId)
    {
        var imported = 0;

        foreach (var transaction in transactions)
        {
            if (await _referenceChecker.ExistsAsync(_dbContext, transaction.Reference, sourceId))
            {
                continue;
            }

            transaction.SourceId = sourceId;

            var source = transaction.Source ?? await _dbContext.TransactionSources.FindAsync(sourceId);
            if (source is null)
            {
                throw new InvalidOperationException($"Transaction source with ID {sourceId} was not found.");
            }

            transaction.Source = source;
            _transactionMapper.ApplyDefaultCategorization(transaction, _categorizationService);

            _dbContext.Transactions.Add(transaction);
            imported++;
        }

        if (imported > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        return imported;
    }

    public async Task<SourceIngestionResult> IngestSourceAsync(TransactionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _logger.LogInformation("Starting ingestion for source {SourceCode} at {BaseUrl}", source.Code, source.BaseUrl);

        return await _ingestionProcessor.ProcessAsync(source);
    }

}