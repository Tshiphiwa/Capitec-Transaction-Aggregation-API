namespace Capitec_Transaction_Aggregation_API.Models;

public enum TransactionDirection
{
    Debit,
    Credit
}

public enum TransactionType
{
    CardSwipe,
    EftTransfer,
    DebitOrder,
    AtmWithdrawal,
    SalaryCredit
}

public enum CategorySource
{
    MccCode,
    Keyword,
    Manual,
    Uncategorised
}

public enum UserRole
{
    Admin,
    Analyst
}