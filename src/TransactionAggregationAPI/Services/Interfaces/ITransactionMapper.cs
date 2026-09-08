using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ITransactionMapper
{
    TransactionDto MapToDto(Transaction transaction);
    TransactionType MapTransactionType(string rawType);
    TransactionDirection MapTransactionDirection(string rawDirection);
}