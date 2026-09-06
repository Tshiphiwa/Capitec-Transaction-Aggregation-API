using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Capitec_Transaction_Aggregation_API.MockSources;

namespace Capitec_Transaction_Aggregation_API.Services;

public class IngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly CategorizationService _categorizationService;
    private readonly ILogger<IngestionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public IngestionService(AppDbContext dbContext, CategorizationService categorizationService, ILogger<IngestionService> logger, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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
        return result;
    }

    public async Task<SourceIngestionResult> IngestSourceAsync(TransactionSource source)
    {

        _logger.LogInformation("Starting ingestion for source {SourceCode} at {BaseUrl}", source.Code, source.BaseUrl);

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{source.BaseUrl}/api/transactions");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var rawTransactions = JsonSerializer.Deserialize<List<TransactionDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<TransactionDto>();

        var ingested = 0;
        var skipped = 0;

        foreach (var raw in rawTransactions)
        {
                // Check for duplicates based on Reference and SourceId
                var exists = await _dbContext.Transactions.AnyAsync(t => t.Reference == raw.Reference && t.SourceId == source.Id);
                
                if (exists)
                {
                    skipped++;
                    continue;
                }
         
                _dbContext.Transactions.Add(MapToTransaction(raw, source));
                ingested++;

            if (ingested > 0)
            {
                await _dbContext.SaveChangesAsync();
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

    private Transaction MapToTransaction(RawTransactionDto raw, TransactionSource source)
    {
        var (category, categorySource) = _categorizationService.Categorize(raw.MccCode, raw.Description);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = raw.Amount,
            Currency = raw.Currency,
            Description = raw.Description,
            MerchantName = raw.MerchantName,
            MccCode = raw.MccCode,
            Category = category,
            CategorySource = categorySource,
            TransactionType = MapTransactionType(raw.TransactionType),
            Direction = raw.Direction,
            TransactionDate = raw.TransactionDate,
            Reference = raw.Reference,
            FromAccount = raw.FromAccount,
            ToAccount = raw.ToAccount,
            SourceId = source.Id,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
    }

    private static TransactionType MapTransactionType(string rawType) =>
        rawType?.ToLowerInvariant() switch
        {
            "CARD_SWIPE" => TransactionType.CardSwipe,
            "EFT_CREDIT" => TransactionType.EftTransfer,
            "EFT_DEBIT" => TransactionType.EftTransfer,
            "SALARY_CREDIT" => TransactionType.SalaryCredit,
            "ATM_WITHDRAWAL" => TransactionType.AtmWithdrawal,
            "WALLET_PAYMENT" => TransactionType.EftTransfer,
            "WALLET_TRANSFER" => TransactionType.EftTransfer,
            "WALLET_TOPUP" => TransactionType.EftTransfer,
            _ => TransactionType.EftTransfer
        };

        private static TransactionDirection MapTransactionDirection(string rawDirection) =>
        rawDirection?.ToLowerInvariant() switch{
            "CREDIT" => TransactionDirection.Credit,
            _ => TransactionDirection.Debit
        };

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
}