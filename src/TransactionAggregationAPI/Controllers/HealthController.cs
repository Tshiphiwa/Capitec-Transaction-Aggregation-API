using System.Reflection;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

/// <summary>
/// Provides health and readiness status for the API and its backing database.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var health = new HealthDto
        {
            Status = "Healthy",
            Message = "API is running normally",
            TimeStamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            Components = new Dictionary<string, string>
            {
                ["application"] = "Up",
                ["database"] = _dbContext.Database.CanConnect() ? "Up" : "Down",
                ["auth"] = "Enabled"
            }
        };

        var dbHealthy = false;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync();
            health.Components["database"] = dbHealthy ? "Up" : "Down";
        }
        catch (Exception ex)
        {
            health.Message = $"Database connection failed: {ex.Message}";
        }

        health.Status = dbHealthy ? "Healthy" : "Unhealthy";

        return dbHealthy ? Ok(health) : StatusCode(503, health);
    }
}