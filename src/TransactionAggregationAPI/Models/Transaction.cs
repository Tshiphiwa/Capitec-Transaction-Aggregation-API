using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Capitec_Transaction_Aggregation_API.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Description { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    // Only present on card transactions - eft/atm/salary has no MCC
    public string? MccCode { get; set; }
    public string Category { get; set; } = "Uncategorised";
    public CategorySource  CategorySource { get; set; } = CategorySource.Uncategorised;
    public TransactionType TransactionType { get; set; }
    public TransactionDirection Direction { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Reference { get; set; } = string.Empty;
    // Will only populate this for eft transfers
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public Guid SourceId { get; set; } = Guid.Empty;
    public virtual TransactionSource Source { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
}