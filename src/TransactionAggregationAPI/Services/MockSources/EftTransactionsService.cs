using Capitec_Transaction_Aggregation_API.DTOs;

namespace Capitec_Transaction_Aggregation_API.Services;

public class EftTransactionsService : IEftTransactionsService
{
    public IReadOnlyList<RawTransactionDto> GetTransactions() =>
    [
        // Month 1: salary + expenses
        new RawTransactionDto
        {
            Reference = "EFT-2001",
            Amount = 25000.00m,
            Currency = "ZAR",
            Description = "SALARY PAYMENT ABSA BANK",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-30),
            MerchantName = "ABSA Payroll",
            TransactionType = "SALARY_CREDIT",
            Direction = "CREDIT",
            FromAccount = "****5678",
            ToAccount = "****1234",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2002",
            Amount = 12000.00m,
            Currency = "ZAR",
            Description = "RENT PAYMENT FOURWAYS PROPERTIES",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-29),
            MerchantName = "Fourways Properties",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****9087",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2003",
            Amount = 1450.00m,
            Currency = "ZAR",
            Description = "DISCOVERY HEALTH INSURANCE PREMIUM",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-28),
            MerchantName = "Discovery Health",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****7711",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2004",
            Amount = 3500.00m,
            Currency = "ZAR",
            Description = "TRANSFER TO SAVINGS ACCOUNT",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-27),
            MerchantName = "Savings Transfer",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****3344",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2005",
            Amount = 170.00m,
            Currency = "ZAR",
            Description = "VODACOM AIRTIME RECHARGE",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-26),
            MerchantName = "Vodacom",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****8800",
        },

        // Month 2
        new RawTransactionDto
        {
            Reference = "EFT-2006",
            Amount = 25000.00m,
            Currency = "ZAR",
            Description = "SALARY PAYMENT ABSA BANK",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-60),
            MerchantName = "ABSA Payroll",
            TransactionType = "SALARY_CREDIT",
            Direction = "CREDIT",
            FromAccount = "****5678",
            ToAccount = "****1234",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2007",
            Amount = 12000.00m,
            Currency = "ZAR",
            Description = "RENT PAYMENT FOURWAYS PROPERTIES",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-59),
            MerchantName = "Fourways Properties",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****9087",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2008",
            Amount = 4250.00m,
            Currency = "ZAR",
            Description = "SCHOOL FEES CURRO PAYMENT",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-58),
            MerchantName = "Curro",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****1415",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2009",
            Amount = 2800.00m,
            Currency = "ZAR",
            Description = "CAPITEC LOAN INSTALLMENT",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-57),
            MerchantName = "Capitec Loan",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****2222",
        },

        // Month 3
        new RawTransactionDto
        {
            Reference = "EFT-2010",
            Amount = 25000.00m,
            Currency = "ZAR",
            Description = "SALARY PAYMENT ABSA BANK",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-90),
            MerchantName = "ABSA Payroll",
            TransactionType = "SALARY_CREDIT",
            Direction = "CREDIT",
            FromAccount = "****5678",
            ToAccount = "****1234",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2011",
            Amount = 12000.00m,
            Currency = "ZAR",
            Description = "RENT PAYMENT FOURWAYS PROPERTIES",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-89),
            MerchantName = "Fourways Properties",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****9087",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2012",
            Amount = 799.00m,
            Currency = "ZAR",
            Description = "DSTV SUBSCRIPTION PAYMENT",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-88),
            MerchantName = "DSTV",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****3333",
        },
        new RawTransactionDto
        {
            Reference = "EFT-2013",
            Amount = 650.00m,
            Currency = "ZAR",
            Description = "GYM MEMBERSHIP VIRGIN ACTIVE",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-87),
            MerchantName = "Virgin Active",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****4444",
        },

        // Matches no keyword - uncategorised
        new RawTransactionDto
        {
            Reference = "EFT-2014",
            Amount = 480.00m,
            Currency = "ZAR",
            Description = "PAYMENT REF 873Y83 ONLINE",
            MccCode = "0000",
            TransactionDate = DateTime.UtcNow.AddDays(-55),
            MerchantName = "Uncategorised",
            TransactionType = "EFT_TRANSFER",
            Direction = "DEBIT",
            FromAccount = "****1234",
            ToAccount = "****9999",
        }
    ];
}
