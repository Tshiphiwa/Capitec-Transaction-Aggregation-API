using System.Text.Json;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Services;

public class IngestionService : IIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICategorizationService _categorizationService;
    private readonly ITransactionMapper _transactionMapper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IngestionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public IngestionService(
        AppDbContext dbContext,
        ICategorizationService categorizationService,
        IHttpClientFactory httpClientFactory,
        ITransactionMapper transactionMapper,
        ILogger<IngestionService> logger)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _httpClientFactory = httpClientFactory;
        _transactionMapper = transactionMapper;
        _logger = logger;
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

    public async Task<SourceIngestionResult> IngestSourceAsync(TransactionSource source)
    {
        _logger.LogInformation("Starting ingestion for source {SourceCode} at {BaseUrl}", source.Code, source.BaseUrl);

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{source.BaseUrl}/transactions");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var rawTransactions = JsonSerializer.Deserialize<List<RawTransactionDto>>(json, JsonOptions)
            ?? [];

        // Load all existing references for this source in 1 query to avoid N round trips
        var existingReferences = await _dbContext.Transactions
            .Where(t => t.SourceId == source.Id)
            .Select(t => t.Reference)
            .ToHashSetAsync();

        var ingested = 0;
        var skipped = 0;

        foreach (var raw in rawTransactions)
        {
            if (existingReferences.Contains(raw.Reference))
            {
                skipped ++;
                continue;
            }

            _dbContext.Transactions.Add(MapToTransaction(raw, source));
            ingested++;
        }

        if (ingested > 0)
            await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
        "Source: {SourceCode} - {Ingested} ingested, {Skipped} skipped",
        source.Code, ingested, skipped);
        
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
        var(category, categorySource) = _categorizationService.Categorize(raw.MccCode, raw.Description);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = raw.Amount,
            Currency = raw.Currency,
            Description = raw.Description,
            MerchantName = raw.MerchantName,
            MccCode = raw.MccCode,
            Category = category,
            CategorySource = categorySource,
            TransactionType = _transactionMapper.MapTransactionType(raw.TransactionType),
            Direction = _transactionMapper.MapTransactionDirection(raw.Direction),
            TransactionDate = raw.TransactionDate,
            Reference = raw.Reference,
            FromAccount = raw.FromAccount,
            ToAccount = raw.ToAccount,
            SourceId = source.Id,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
    }
}