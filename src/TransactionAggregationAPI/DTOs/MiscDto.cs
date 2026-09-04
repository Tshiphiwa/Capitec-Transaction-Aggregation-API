namespace Capitec_Transaction_Aggregation_API.DTOs;

public class UpdateCategoryDto
{
    public string Category { get; set; } = string.Empty;
}

public class TransactionSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastSyncAt { get; set; }
    public int TransactionCount { get; set; }
}

public class HealthDto
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string> Components { get; set; } = new(); //Tags
}