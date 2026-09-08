using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class HealthService: IHealthService{
    
    private readonly AppDbContext _dbContext;

    public HealthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<(bool IsHealthy, HealthDto Health)> GetHealthAsync()
    {
        var health = new HealthDto
        {
            Status = "Healthy",
            Message = "API is running normally",
            TimeStamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        var dbHealthy = false;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync();
            health.Components["Database"] = dbHealthy ? "Up" : "Down";
        }
        catch
        {
           health.Components["Database"] = "Down";
        }

        health.Status = dbHealthy ? "Healthy" : "Unhealthy";

        return (dbHealthy, health);
    }
}