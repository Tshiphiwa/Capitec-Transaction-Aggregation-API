namespace Capitec_Transaction_Aggregation_API.DTOs;

public class RawTransactionDto
{
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MccCode { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
}
