using Capitec_Transaction_Aggregation_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Infrastructure;

// Will use this file to run once on startup to seed sources and admin user if db is empty
public class DatabaseSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(AppDbContext dbContext, ILogger<DatabaseSeeder> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync();
        }

        await SeedSourceAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedSourceAsync()
    {
        if (await _dbContext.TransactionSources.AnyAsync())
            return;

        var sources = new List<TransactionSource>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Card Processing System",
                Code = "CARD",
                BaseUrl = _configuration["MockSources:Card:BaseUrl"] ?? "http://localhost:5000/api/mock-sources/card",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "EFT Payment System",
                Code = "EFT",
                BaseUrl = _configuration["MockSources:Eft:BaseUrl"] ?? "http://localhost:5000/api/mock-sources/eft",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Digital Wallet System",
                Code = "WALLET",
                BaseUrl = _configuration["MockSources:Wallet:BaseUrl"] ?? "http://localhost:5000/api/mock-sources/wallet",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
            }
        };

        _dbContext.TransactionSources.AddRange(sources);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Seeding database {Count} sources successful", sources.Count);
    }
    private async Task SeedAdminUserAsync()
    {
        if (await _dbContext.Users.AnyAsync())
            return;

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@capitec.com",
            UserName = "admin",
            // BCrypt hash for the default admin password: Password123!
            PasswordHash = "$2b$12$07zXkfI/cBXL/gea58qY6eedQ6sAGtKuBtc1B2Vc.I23NKRiqzKFy",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
        };
        
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Seeded the default admin user: {Email}", adminUser.Email);
    }
}