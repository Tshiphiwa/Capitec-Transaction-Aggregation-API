namespace Capitec_Transaction_Aggregation_API.DTOs;

public class IngestionResultDto
{
    public int TotalIngested { get; set; }
    public int TotalSkipped { get; set; }
    public List<SourceIngestionResult> SourceResults { get; set; } = new();
}

public class SourceIngestionResult
{
    public string SourceCode { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int IngestedCount { get; set; }
    public int SkippedCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
