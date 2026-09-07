using System.Text.Json;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Capitec_Transaction_Aggregation_API.Services;

public class IngestionService : IIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICategorizationService _categorizationService;
    private readonly ILogger<IngestionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public IngestionService(
        AppDbContext dbContext,
        ICategorizationService categorizationService,
        ILogger<IngestionService>? logger = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _logger = logger ?? NullLogger<IngestionService>.Instance;
        _httpClientFactory = httpClientFactory ?? new NullHttpClientFactory();
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

        _logger.LogInformation("Ingestion completed. Total ingested: {TotalIngested}, Total skipped: {TotalSkipped}", result.TotalIngested, result.TotalSkipped);

        return result;
    }

    public async Task<int> ImportTransactionsAsync(IEnumerable<Transaction> transactions, Guid sourceId)
    {
        var imported = 0;

        foreach (var transaction in transactions)
        {
            var exists = await _dbContext.Transactions.AnyAsync(t => t.Reference == transaction.Reference && t.SourceId == sourceId);
            if (exists)
            {
                continue;
            }

            transaction.SourceId = sourceId;

            var sourceEntity = transaction.Source ?? await _dbContext.TransactionSources.FindAsync(sourceId);
            if (sourceEntity is null)
            {
                throw new InvalidOperationException($"Transaction source with ID {sourceId} was not found.");
            }

            transaction.Source = sourceEntity;

            if (string.IsNullOrWhiteSpace(transaction.Category) || string.Equals(transaction.Category, "Uncategorised", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Category = _categorizationService.CategorizeTransaction(transaction);
            }

            if (transaction.CategorySource == default || transaction.CategorySource == CategorySource.Uncategorised)
            {
                transaction.CategorySource = string.IsNullOrWhiteSpace(transaction.MccCode)
                    ? CategorySource.Keyword
                    : CategorySource.MccCode;
            }

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
        _logger.LogInformation("Starting ingestion for source {SourceCode} at {BaseUrl}", source.Code, source.BaseUrl);

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{source.BaseUrl}/api/transactions");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var rawTransactions = JsonSerializer.Deserialize<List<RawTransactionDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RawTransactionDto>();

        var ingested = 0;
        var skipped = 0;

        foreach (var raw in rawTransactions)
        {
            var exists = await _dbContext.Transactions.AnyAsync(t => 
                t.Reference == raw.Reference && t.SourceId == source.Id);

            if (exists)
            {
                skipped++;
                continue;
            }

            _dbContext.Transactions.Add(MapToTransaction(raw, source));
            ingested++;
        }

        if (ingested > 0)
               await _dbContext.SaveChangesAsync();

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

    private Transaction MapToTransaction(RawTransactionDto raw, TransactionSource source)
    {
        var (category, categorySource) = _categorizationService.Categorize(raw.MccCode, raw.Description);
        var transactionType = MapTransactionType(raw.TransactionType);
        var direction = MapTransactionDirection(raw.Direction);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = raw.Amount,
            Currency = string.IsNullOrWhiteSpace(raw.Currency) ? "ZAR" : raw.Currency,
            Description = raw.Description,
            MerchantName = string.IsNullOrWhiteSpace(raw.MerchantName) ? "Unknown Merchant" : raw.MerchantName,
            MccCode = string.IsNullOrWhiteSpace(raw.MccCode) ? null : raw.MccCode,
            Category = category,
            CategorySource = categorySource,
            TransactionType = transactionType,
            Direction = direction,
            TransactionDate = raw.TransactionDate,
            Reference = raw.Reference,
            FromAccount = string.IsNullOrWhiteSpace(raw.FromAccount) ? "Unknown" : raw.FromAccount,
            ToAccount = string.IsNullOrWhiteSpace(raw.ToAccount) ? "Unknown" : raw.ToAccount,
            SourceId = source.Id,
            Source = source,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
    }

    private TransactionType MapTransactionType(string rawType)
    {
        return rawType.ToLowerInvariant() switch
        {
            "CARD_SWIPE" => TransactionType.CardSwipe,
            "EFT_CREDIT" => TransactionType.EftTransfer,
            "EFT_DEBIT" => TransactionType.EftTransfer,
            "WALLET_PAYMENT" => TransactionType.CardSwipe,
            "WALLET_TRANSFER" => TransactionType.EftTransfer,
            "EFT_TRANSFER" => TransactionType.EftTransfer,
            "SALARY_CREDIT" => TransactionType.SalaryCredit,
            "ATM_WITHDRAWAL" => TransactionType.AtmWithdrawal,
            _ => LogAndDefaultToEft(rawType)
        };
    }

    private TransactionDirection MapTransactionDirection(string rawDirection)
    {
        return rawDirection?.ToLowerInvariant() switch
        {
            "CREDIT" => TransactionDirection.Credit,
            _ => LogAndDefaultToDebit(rawDirection)
        };
    }

    private TransactionType LogAndDefaultToEft(string rawType)
    {
        _logger.LogWarning("Unrecognized transaction type '{TransactionType}' received. Defaulting to EFT transfer.", rawType);
        return TransactionType.EftTransfer;
    }

    private TransactionDirection LogAndDefaultToDebit(string? rawDirection)
    {
        _logger.LogWarning("Unrecognized transaction direction '{TransactionDirection}' received. Defaulting to Debit.", rawDirection ?? "<null>");
        return TransactionDirection.Debit;
    }

    public class IngestionResultDto
    {
        public int TotalIngested { get; set; }
        public int TotalSkipped { get; set; }
        public List<SourceIngestionResult> SourceResults { get; set; } = new();
    }

    public class SourceIngestionResult
    {
        public string SourceCode { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int IngestedCount { get; set; }
        public int SkippedCount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    private sealed class NullHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}