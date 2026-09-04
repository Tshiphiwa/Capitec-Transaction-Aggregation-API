namespace Capitec_Transaction_Aggregation_API.Models;

public class TransactionSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}