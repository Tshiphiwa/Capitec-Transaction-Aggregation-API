using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionService
{
    Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId);
    Task<SummaryDto> GetTransactionSummaryAsync(TransactionFilterDto filter);
    Task<AggregatedTransactionDto> GetAggregatedTransactionsAsync(TransactionFilterDto filter);
    Task<TransactionDto> UpdateCategoryAsync(Guid transactionId, string newCategory, UserRole userRole);
}
