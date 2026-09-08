using System.Diagnostics.Tracing;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionMapper _transactionMapper;
    private readonly ILogger<TransactionService> _logger;
    private const int MaxPageSize = 100;

    public TransactionService(AppDbContext dbContext, ITransactionMapper transactionMapper, ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _transactionMapper = _transactionMapper;
        _logger = logger ?? NullLogger<TransactionService>.Instance;
    }

    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
    {
        var pageSize = Math.Min(filter.PageSize, MaxPageSize);
        var page = Math.Max(filter.Page, 1);

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Source)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResultDto<TransactionDto>.Create(
            transactions.Select(_transactionMapper.MapToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Source)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
        }

        return _transactionMapper.MapToDto(transaction);
    }

    public async Task<SummaryDto> GetTransactionSummaryAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsQueryable();
        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync();
        if (totalCount == 0)
            return new SummaryDto { FromDate = filter.From, ToDate = filter. To };

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalDebits = g.Where(t => t.Direction == TransactionDirection.Debit).Sum(t => t.Amount),
                TotalCredits = g.Where(t => t.Direction == TransactionDirection.Credit).Sum(t => t.Amount),
                Average = g.Average(t => t.Amount),
                Count = g.Count()
            })
            .FirstAsync();

        var spendByCategory = await query
            .Where(t => t.Direction == TransactionDirection.Debit)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total);

        var topCategory = spendByCategory.Any()
            ? spendByCategory.OrderByDescending(x => x.Value).First().Key
            : "None";

        return new SummaryDto
        {
            TotalDebits = totals.TotalDebits,
            TotalCredits = totals.TotalCredits,
            NetAmount = totals.TotalCredits - totals.TotalDebits,
            TransactionCount = totals.Count,
            AverageTransactionAmount = totals.Average,
            SpendByCategory = spendByCategory,
            TopSpendingCategory = topCategory,
            FromDate = filter.From,
            ToDate = filter.To
        };
    }

    public async Task<AggregatedTransactionsDto> GetAggregatedTransactionsAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, filter);

        // Only consider debit transactions for aggregation since we are interested in spending patterns not income
        var debitQuery = query.Where(t => t.Direction == TransactionDirection.Debit);    

        var totalCount = await debitQuery.CountAsync();
        if (totalCount == 0)
            return new AggregatedTransactionsDto { FromDate = filter.From, ToDate = filter.To };

        var grandTotal = await debitQuery.SumAsync(t => t.Amount);

        var categories = await debitQuery
            .GroupBy(t => t.Category)
            .Select(g => new AggregatedCategoryDto
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                PercentageOfTotalSpend = grandTotal > 0 ? (g.Sum(t => t.Amount) / grandTotal) * 100 : 0,
                AverageTransactionAmount = Math.Round(g.Average(t => t.Amount), 2),
                LargestTransaction = g.Max(t => t.Amount),
                LastTransactionDate = g.Max(t => t.TransactionDate)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToListAsync();

        return new AggregatedTransactionsDto
        {
            Categories = categories,
            GrandTotal = grandTotal,
            TotalTransactions = totalCount,
            FromDate = filter.From,
            ToDate = filter.To
        };
    }

    public async Task<TransactionDto> UpdateCategoryAsync(Guid transactionId, string newCategory)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Source)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
        }

        var oldCategory = transaction.Category;
        transaction.Category = newCategory;
        transaction.CategorySource = CategorySource.Manual;
        transaction.LastUpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} category updated from {OldCategory} to {NewCategory}", transactionId, oldCategory, newCategory);

        return _transactionMapper.MapToDto(transaction);
    }

    public async Task<List<TransactionSourceDto>> GetSourcesAsync()
    {
        return await _dbContext.TransactionSources
            .Select(s => new TransactionSourceDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                IsActive = s.IsActive,
                LastSyncAt = s.LastUpdatedDate,
                TransactionCount = s.Transactions.Count()
            })
            .ToListAsync();
    }

    private IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(t => t.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter.SourceCode))
            query = query.Where(t => t.Source.Code == filter.SourceCode);

        if (Enum.TryParse<TransactionType>(filter.TransactionType, out var txType))
            query = query.Where(t => t.TransactionType == txType);

        if (Enum.TryParse<TransactionDirection>(filter.Direction, out var direction))
            query = query.Where(t => t.Direction == direction);

        if (filter.From.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.To.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= filter.MinAmount);

        if (filter.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= filter.MaxAmount);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(t =>
                t.Description.ToLower().Contains(search) ||
                (t.MerchantName != null && t.MerchantName.ToLower().Contains(search)));
        }

        return query;
    }
}