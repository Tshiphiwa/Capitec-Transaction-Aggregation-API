using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionMapper : ITransactionMapper
{
    public Transaction MapToTransaction(RawTransactionDto raw, TransactionSource source, ICategorizationService categorizationService, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(categorizationService);

        var (category, categorySource) = categorizationService.Categorize(raw.MccCode, raw.Description);

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
            TransactionType = MapTransactionType(raw.TransactionType, logger),
            Direction = MapTransactionDirection(raw.Direction, logger),
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

    public void ApplyDefaultCategorization(Transaction transaction, ICategorizationService categorizationService)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(categorizationService);

        if (string.IsNullOrWhiteSpace(transaction.Category) || string.Equals(transaction.Category, "Uncategorised", StringComparison.OrdinalIgnoreCase))
        {
            transaction.Category = categorizationService.CategorizeTransaction(transaction);
        }

        if (transaction.CategorySource == default || transaction.CategorySource == CategorySource.Uncategorised)
        {
            transaction.CategorySource = string.IsNullOrWhiteSpace(transaction.MccCode)
                ? CategorySource.Keyword
                : CategorySource.MccCode;
        }
    }

    public TransactionType MapTransactionType(string rawType, ILogger logger)
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
            _ => LogAndDefaultToEft(rawType, logger)
        };
    }

    public TransactionDirection MapTransactionDirection(string rawDirection, ILogger logger)
    {
        return rawDirection?.ToLowerInvariant() switch
        {
            "CREDIT" => TransactionDirection.Credit,
            _ => LogAndDefaultToDebit(rawDirection, logger)
        };
    }

    private static TransactionType LogAndDefaultToEft(string rawType, ILogger logger)
    {
        logger.LogWarning("Unrecognized transaction type '{TransactionType}' received. Defaulting to EFT transfer.", rawType);
        return TransactionType.EftTransfer;
    }

    private static TransactionDirection LogAndDefaultToDebit(string? rawDirection, ILogger logger)
    {
        logger.LogWarning("Unrecognized transaction direction '{TransactionDirection}' received. Defaulting to Debit.", rawDirection ?? "<null>");
        return TransactionDirection.Debit;
    }
}
