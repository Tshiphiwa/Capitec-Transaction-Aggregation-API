namespace Capitec_Transaction_Aggregation_API.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string MccCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategorySource { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? FromAccount { get; set; }
    public string? ToAccount { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}