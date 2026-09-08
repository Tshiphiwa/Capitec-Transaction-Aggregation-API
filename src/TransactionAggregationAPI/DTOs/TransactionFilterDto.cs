using System.ComponentModel.DataAnnotations;

namespace Capitec_Transaction_Aggregation_API.DTOs;

public class TransactionFilterDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; } = string.Empty;
    public string? SourceCode { get; set; } = string.Empty;
    public string? TransactionType { get; set; } = string.Empty;
    public string? Direction { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(0, double.MinValue, ErrorMessage = "Min Amount must be a positive value")]
    public decimal? MinAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Max Amount must be a positive value")]
    public decimal? MaxAmount { get; set; }
    public string? Search { get; set; } = string.Empty;
}