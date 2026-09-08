using Capitec_Transaction_Aggregation_API.DTOs;

namespace Capitec_Transaction_Aggregation_API.MockSources.Data;

public static class CardTransactionData
{
    public static IReadOnlyList<RawTransactionDto> Get() =>
    [        new ()
        {
            Reference = "CARD-1001",
            Amount = 189.90m,
            Currency = "ZAR",
            Description = "KFC SANDTON CITY",
            MccCode = "5814",
            TransactionDate = DateTime.UtcNow.AddDays(-1),
            MerchantName = "KFC Sandton City",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1002",
            Amount = 648.75m,
            Currency = "ZAR",
            Description = "WOOLWORTHS FOOD ROSEBANK",
            MccCode = "5411",
            TransactionDate = DateTime.UtcNow.AddDays(-2),
            MerchantName = "WOOLWORTHS FOOD ROSEBANK",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1003",
            Amount = 820.00m,
            Currency = "ZAR",
            Description = "SHELL GARAGE SANDTON",
            MccCode = "5541",
            TransactionDate = DateTime.UtcNow.AddDays(-3),
            MerchantName = "SHELL GARAGE SANDTON",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1004",
            Amount = 169.00m,
            Currency = "ZAR",
            Description = "NETFLIX.COM",
            MccCode = "7841",
            TransactionDate = DateTime.UtcNow.AddDays(-4),
            MerchantName = "NETFLIX",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1005",
            Amount = 1424.80m,
            Currency = "ZAR",
            Description = "PICK N PAY HYPER CRESTA",
            MccCode = "5411",
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            MerchantName = "PICK N PAY HYPER CRESTA",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1006",
            Amount = 2199.00m,
            Currency = "ZAR",
            Description = "TAKEALOT ONLINE PURCHASE",
            MccCode = "5967",
            TransactionDate = DateTime.UtcNow.AddDays(-6),
            MerchantName = "TAKEALOT ONLINE PURCHASE",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1007",
            Amount = 760.50m,
            Currency = "ZAR",
            Description = "ENGEN PETROL STATION MIDRAND",
            MccCode = "5541",
            TransactionDate = DateTime.UtcNow.AddDays(-7),
            MerchantName = "ENGEN PETROL STATION MIDRAND",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1008",
            Amount = 485.00m,
            Currency = "ZAR",
            Description = "NANDOS FOURWAYS MALL",
            MccCode = "5812",
            TransactionDate = DateTime.UtcNow.AddDays(-8),
            MerchantName = "NANDOS FOURWAYS MALL",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1009",
            Amount = 240.00m,
            Currency = "ZAR",
            Description = "UBER TRIP JHB",
            MccCode = "4121",
            TransactionDate = DateTime.UtcNow.AddDays(-9),
            MerchantName = "UBER TRIP JHB",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        },
        new ()
        {
            Reference = "CARD-1010",
            Amount = 1320.45m,
            Currency = "ZAR",
            Description = "SPORTMANS WAREHOUSE CLEARWATER",
            MccCode = "5941",
            TransactionDate = DateTime.UtcNow.AddDays(-10),
            MerchantName = "SPORTMANS WAREHOUSE CLEARWATER",
            TransactionType = "CARD_SWIPE",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = string.Empty,
        }];
}