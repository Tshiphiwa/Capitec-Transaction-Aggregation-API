using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface IHealthService
{
    Task<(bool IsHealthy, HealthDto Health)> GetHealthAsync();
}
