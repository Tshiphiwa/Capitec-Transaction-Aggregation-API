using Capitec_Transaction_Aggregation_API.DTOs;

namespace Capitec_Transaction_Aggregation_API.Services;

public class WalletTransactionsService : IWalletTransactionsService
{
    public IReadOnlyList<RawTransactionDto> GetTransactions() =>
    [
        new RawTransactionDto
        {
            Reference = "WALLET-3001",
            Amount = 245.60m,
            Currency = "ZAR",
            Description = "UBER EATS ORDER DELIVERY",
            MccCode = "5812",
            TransactionDate = DateTime.UtcNow.AddDays(-1),
            MerchantName = "Uber Eats",
            TransactionType = "WALLET_PAYMENT",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = string.Empty,
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3002",
            Amount = 1500.00m,
            Currency = "ZAR",
            Description = "WALLET TOP UP FROM BANK ACCOUNT",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-2),
            MerchantName = "Bank Transfer",
            TransactionType = "WALLET_TRANSFER",
            Direction = "CREDIT",
            FromAccount = "****1122",
            ToAccount = "wallet-01",
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3003",
            Amount = 350.00m,
            Currency = "ZAR",
            Description = "SEND MONEY TO SIPHO",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-3),
            MerchantName = "Sipho Transfer",
            TransactionType = "WALLET_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = "sipho-wallet",
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3004",
            Amount = 189.90m,
            Currency = "ZAR",
            Description = "STEERS BURGER RESTAURANT ONLINE",
            MccCode = "5814",
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            MerchantName = "Steers",
            TransactionType = "WALLET_PAYMENT",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = string.Empty,
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3005",
            Amount = 800.00m,
            Currency = "ZAR",
            Description = "MONEY RECEIVED FROM MOM",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-6),
            MerchantName = "Family Transfer",
            TransactionType = "WALLET_TRANSFER",
            Direction = "CREDIT",
            FromAccount = "mom-wallet",
            ToAccount = "wallet-01",
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3006",
            Amount = 129.00m,
            Currency = "ZAR",
            Description = "APPLE APP STORE PURCHASE",
            MccCode = "5815",
            TransactionDate = DateTime.UtcNow.AddDays(-8),
            MerchantName = "Apple App Store",
            TransactionType = "WALLET_PAYMENT",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = string.Empty,
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3007",
            Amount = 65.00m,
            Currency = "ZAR",
            Description = "PARKING FEE SANDTON",
            MccCode = "7523",
            TransactionDate = DateTime.UtcNow.AddDays(-10),
            MerchantName = "Sandton Parking",
            TransactionType = "WALLET_PAYMENT",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = string.Empty,
        },
        new RawTransactionDto
        {
            Reference = "WALLET-3008",
            Amount = 620.00m,
            Currency = "ZAR",
            Description = "MR PRICE ONLINE SHOPPING",
            MccCode = "5651",
            TransactionDate = DateTime.UtcNow.AddDays(-12),
            MerchantName = "Mr Price",
            TransactionType = "WALLET_PAYMENT",
            Direction = "DEBIT",
            FromAccount = "wallet-01",
            ToAccount = string.Empty,
        }
    ];
}
