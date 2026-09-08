using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionMapper : ITransactionMapper
{
    public TransactionDto MapToDto(Transaction t) => new ()
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
            SourceName = t.Source?.Name ?? string.Empty,
            SourceCode = t.Source?.Code ?? string.Empty,
            CreatedAt = t.CreatedDate
    };

    public TransactionType MapTransactionType(string tType)
    {
        return tType.ToLowerInvariant() switch
        {
            "card_swipe" => TransactionType.CardSwipe,
            "eft_credit" => TransactionType.EftTransfer,
            "eft_debit" => TransactionType.EftTransfer,
            "wallet_payment" => TransactionType.CardSwipe,
            "wallet_transfer" => TransactionType.EftTransfer,
            "eft_transfer" => TransactionType.EftTransfer,
            "salary_credit" => TransactionType.SalaryCredit,
            "atm_withdrawal" => TransactionType.AtmWithdrawal,
            _ => TransactionType.EftTransfer
        };
    }

    public TransactionDirection MapTransactionDirection(string tDirection)
    {
        return tDirection?.ToLowerInvariant() switch
        {
            "credit" => TransactionDirection.Credit,
            _ => TransactionDirection.Debit
        };
    }
}
