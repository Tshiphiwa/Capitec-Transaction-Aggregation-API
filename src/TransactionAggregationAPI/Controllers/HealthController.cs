using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

/// <summary>
/// Provides health and readiness status for the API and its backing database.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Checks whether the API and its database dependencies are healthy.
    /// </summary>
    /// <returns>A health payload indicating the current application and database status.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        var (isHealthy, health) = await _healthService.GetHealthAsync();

        return isHealthy ? Ok(health) : StatusCode(StatusCodes.Status503ServiceUnavailable, health);
    }
}