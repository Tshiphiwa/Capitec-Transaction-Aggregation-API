using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionIngestionProcessor : ITransactionIngestionProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly ICategorizationService _categorizationService;
    private readonly ITransactionSourceClient _transactionSourceClient;
    private readonly ITransactionReferenceChecker _referenceChecker;
    private readonly ITransactionMapper _transactionMapper;
    private readonly ILogger<TransactionIngestionProcessor> _logger;

    public TransactionIngestionProcessor(
        AppDbContext dbContext,
        ICategorizationService categorizationService,
        ITransactionSourceClient transactionSourceClient,
        ITransactionReferenceChecker referenceChecker,
        ITransactionMapper transactionMapper,
        ILogger<TransactionIngestionProcessor> logger)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _transactionSourceClient = transactionSourceClient;
        _referenceChecker = referenceChecker;
        _transactionMapper = transactionMapper;
        _logger = logger;
    }

    public async Task<SourceIngestionResult> ProcessAsync(TransactionSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        _logger.LogInformation("Starting ingestion for source {SourceCode} at {BaseUrl}", source.Code, source.BaseUrl);

        var rawTransactions = await _transactionSourceClient.GetTransactionsAsync(source, cancellationToken);
        var ingested = 0;
        var skipped = 0;

        foreach (var raw in rawTransactions)
        {
            if (await _referenceChecker.ExistsAsync(_dbContext, raw.Reference, source.Id))
            {
                skipped++;
                continue;
            }

            _dbContext.Transactions.Add(_transactionMapper.MapToTransaction(raw, source, _categorizationService, _logger));
            ingested++;
        }

        if (ingested > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Ingested {IngestedCount} transactions for source {SourceCode}", ingested, source.Code);

        return new SourceIngestionResult
        {
            SourceCode = source.Code,
            SourceName = source.Name,
            Success = true,
            IngestedCount = ingested,
            SkippedCount = skipped
        };
    }
}
