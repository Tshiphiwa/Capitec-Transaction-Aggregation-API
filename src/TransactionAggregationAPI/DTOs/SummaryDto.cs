namespace Capitec_Transaction_Aggregation_API.DTOs;

public class SummaryDto
{
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public Dictionary<string, decimal> SpendByCategory { get; set; } = new();
    public string TopSpendingCategory { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class AggregatedCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public decimal PercentageOfTotalSpend { get; set; }
    public decimal LargestTransaction { get; set; }
    public DateTime LastTransactionDate { get; set; }
}

public class AggregatedTransactionDto
{
    public List<AggregatedCategoryDto> Categories { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}