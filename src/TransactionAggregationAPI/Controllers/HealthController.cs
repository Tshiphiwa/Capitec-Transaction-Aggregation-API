using System.Reflection;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [HttpGet]
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