using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;
    private const int MaxPageSize = 100;

    public TransactionService(AppDbContext dbContext, ILogger<TransactionService>? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger ?? NullLogger<TransactionService>.Instance;
    }

    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
    {
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, MaxPageSize);
        var page = Math.Max(filter.Page, 1);

        var query = _dbContext.Transactions
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
            transactions.Select(MapToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Source)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
        }

        return MapToDto(transaction);
    }

    public async Task<SummaryDto> GetTransactionSummaryAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsQueryable();
        query = ApplyFilters(query, filter);

        var transactions = await query.ToListAsync();

        if (!transactions.Any())
        {
            return new SummaryDto
            {
                FromDate = filter.From,
                ToDate = filter.To
            };
        }

        var debits = transactions.Where(t => t.Direction == TransactionDirection.Debit).ToList();
        var credits = transactions.Where(t => t.Direction == TransactionDirection.Credit).ToList();

        var totalDebits = debits.Sum(t => t.Amount);
        var totalCredits = credits.Sum(t => t.Amount);

        var spendByCategory = debits
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var topSpendingCategory = spendByCategory.Any()
            ? spendByCategory.OrderByDescending(x => x.Value).First().Key
            : "None";

        return new SummaryDto
        {
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            NetAmount = totalCredits - totalDebits,
            TransactionCount = transactions.Count,
            AverageTransactionAmount = transactions.Average(t => t.Amount),
            SpendByCategory = spendByCategory,
            TopSpendingCategory = topSpendingCategory,
            FromDate = filter.From,
            ToDate = filter.To
        };
    }

    public async Task<AggregatedTransactionDto> GetAggregatedTransactionsAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsQueryable();
        query = ApplyFilters(query, filter);

        // Only consider debit transactions for aggregation since we are interested in spending patterns not income
        var transactions = await query
            .Where(t => t.Direction == TransactionDirection.Debit)
            .ToListAsync();

        if (!transactions.Any())
        {
            return new AggregatedTransactionDto
            {
                FromDate = filter.From,
                ToDate = filter.To
            };
        }

        var grandTotal = transactions.Sum(t => t.Amount);

        var categories = transactions
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
            .ToList();

        return new AggregatedTransactionDto
        {
            Categories = categories,
            GrandTotal = grandTotal,
            TotalTransactions = transactions.Count,
            FromDate = filter.From,
            ToDate = filter.To
        };
    }

    public async Task<TransactionDto> UpdateCategoryAsync(Guid transactionId, string newCategory, UserRole userRole)
    {
        if (userRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Admin users can update transaction categories.");
        }

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

        _logger.LogInformation("Transaction {TransactionId} category updated from {OldCategory} to {NewCategory} by user with role {UserRole}", transactionId, oldCategory, newCategory, userRole);

        return MapToDto(transaction);
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

        if (!string.IsNullOrWhiteSpace(filter.TransactionType))
            query = query.Where(t => t.TransactionType.ToString() == filter.TransactionType);

        if (!string.IsNullOrWhiteSpace(filter.Direction))
            query = query.Where(t => t.Direction.ToString() == filter.Direction);

        if (filter.From != default)
            query = query.Where(t => t.TransactionDate >= filter.From);

        if (filter.To != default)
            query = query.Where(t => t.TransactionDate <= filter.To);

        if (filter.MinAmount != 0)
            query = query.Where(t => t.Amount >= filter.MinAmount);

        if (filter.MaxAmount != 0)
            query = query.Where(t => t.Amount <= filter.MaxAmount);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(t =>
                (t.Description != null && t.Description.Contains(search)) ||
                (t.MerchantName != null && t.MerchantName.Contains(search)) ||
                (t.Reference != null && t.Reference.Contains(search)));
        }

        return query;
    }

    private static TransactionDto MapToDto(Transaction t) => new()
    {
        Id = t.Id,
        Amount = t.Amount,
        Currency = t.Currency,
        Description = t.Description,
        MerchantName = t.MerchantName ?? string.Empty,
        MccCode = t.MccCode ?? string.Empty,
        Category = t.Category,
        CategorySource = t.CategorySource.ToString(),
        TransactionType = t.TransactionType.ToString(),
        Direction = t.Direction.ToString(),
        TransactionDate = t.TransactionDate,
        Reference = t.Reference,
        FromAccount = t.FromAccount,
        ToAccount = t.ToAccount,
        SourceCode = t.Source?.Code ?? string.Empty,
        SourceName = t.Source?.Name ?? string.Empty,
        CreatedAt = t.CreatedDate
    };
}