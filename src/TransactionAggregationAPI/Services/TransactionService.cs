using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;
    private const int MaxPageSize = 100;

    public TransactionService(AppDbContext dbContext, ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
    {
        var pageSize = Math.Min(filter.PageSize, MaxPageSize);
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

        return PagedResultDto<TransactionDto>.Create(transactions.Select(MapToDto).ToList(), totalCount, page, pageSize);
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

    public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsQueryable();
        query = ApplyFilters(query, filter);

        var transactions = await query.ToListAsync();

        if (!transactions.Any())
            return new TransactionSummaryDto { FromDate = filter.From, ToDate = filter.To };

            var debits = transactions.Where(t => t.Direction == TransactionDirection.Debit).ToList();
            var credits = transactions.Where(t => t.Direction == TransactionDirection.Credit).ToList();

            var totalDebits = debits.Sum(t => t.Amount);
            var totalCredits = credits.Sum(t => t.Amount);

            var spendByCategory = debits
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
            
            var topCategory = spendByCategory.Any()
            ? spendByCategory.OrderByDescending(x => x.Value).First().Key
            : "None";


        return new TransactionSummaryDto
        {
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            NetAmount = totalCredits - totalDebits,
            TransactionCount = transactions.Count,
            AverageTransactionAmount = transactions.Average(t => t.Amount),
            SpendByCategory = spendByCategory,
            TopCategory = topCategory,
            FromDate = filter.From,
            ToDate = filter.To
  
        };
    }

    public async Task<AggregatedTransactionDto> GetAggregatedTransactionsAsync(TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions.AsQueryable();
        query = ApplyFilters(query, filter);

// only aggregate debits since credits are income not spend
        var transactions = await query
         .Where(t => t.Direction == TransactionDirection.Debit)
         .ToListAsync();

        if (!transactions.Any())
            return new AggregatedTransactionDto { FromDate = filter.From, ToDate = filter.To  }; 

        var grandTotal = transactions.Sum(t => t.Amount);

        var categories = transactions
            .GroupBy(t => t.Category)
            .Select(g => new CategoryAggregationDto
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                PercentageOfTotal = grandTotal > 0 ? (g.Sum(t => t.Amount) / grandTotal) * 100 : 0,
                AverageTransactionAmount = Math.Round(g.Average(t => t.Amount), 2),
                LargestTransaction = g.Max(t => t.Amount),
                LastTransactionDate = g.Max(t => t.TransactionDate)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();
    }

    private async Task<TransactionDto> UpdateCategoryAsync(Guid transactionId, string newCategory, UserRole userRole)
    {
        if(userRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Admin users can update transaction categories.");
        }

        var transaction = await _dbContext.Transactions
        .Include(t => t.Source)
        .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found.");
        };

        var oldCategory = transaction.Category;
        transaction.Category = newCategory;
        transaction.CategorySource = CategorySource.Manual;
        transaction.LastUpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} category updated from {OldCategory} to {NewCategory} by user with role {UserRole}", transactionId, oldCategory, newCategory, userRole);

        return MapToDto(transaction);
    }

    public async Task<List<TransactionDto>> GetSourcesAsync()
    {
       return await _dbContext.TransactionSources
            .Select(s => new TransactionSourceDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                IsActive = s.IsActive,
                LastUpdatedDate = s.LastUpdatedDate,
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

        if (filter.From.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.To.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= filter.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(filter.SourceCode))
            query = query.Where(t => t.Source.Code == filter.SourceCode);

        if (!string.IsNullOrWhiteSpace(filter.Search)){
            var search =filter.Search.ToLower();
            query = query.Where(t => 
               t.Description.ToLower().Contains(search) ||
               t.MerchantName != null && t.MerchantName.ToLower().Contains(search));

        }
      

        return query;
    }

    private static TransactionDto MapToDto(Transaction t) => new()
    {

            Id = t.Id,
            Amount = t.Amount,
            Currency = t.Currency,
            Description = t.Description,
            MerchantName = t.MerchantName,
            MccCode = t.MccCode,
            Category = t.Category,
            CategorySource = t  .CategorySource.ToString(),
            TransactionType = t.TransactionType.ToString(),
            Direction = t.Direction.ToString(),
            TransactionDate = t.TransactionDate,
            Reference = t.Reference,
            FromAccount = t         .FromAccount,
            ToAccount = t.ToAccount,
            SourceCode = t.Source?.Code ?? string.Empty,
            SourceName = t.Source?.Name ?? string.Empty,
            CreatedDate = t.CreatedDate,
            LastUpdatedDate = t.LastUpdatedDate
        };
}