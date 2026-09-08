using Microsoft.EntityFrameworkCore;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryHours { get; init; } = 8;
}