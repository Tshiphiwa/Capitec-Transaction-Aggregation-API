namespace Capitec_Transaction_Aggregation_API.DTOs;

public class TransactionFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string Category { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Search { get; set; } = string.Empty;
}