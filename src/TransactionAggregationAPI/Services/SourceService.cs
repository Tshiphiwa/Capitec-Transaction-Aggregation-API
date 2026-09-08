using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Capitec_Transaction_Aggregation_API.Services;

public class SourceService : ISourceService
{
    private readonly AppDbContext _dbContext;

    public SourceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TransactionSourceDto>> GetSourcesAsync()
    {
        return await _dbContext.TransactionSources
            .Select(s => new TransactionSourceDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                IsActive = s.IsActive,
                LastSyncAt = s.LastUpdatedDate,
                TransactionCount = s.Transactions.Count()
            })
            .ToListAsync();
    }
}