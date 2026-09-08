using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;

namespace Capitec_Transaction_Aggregation_API.Services.Interfaces;

public interface ISourceService
{
    Task<List<TransactionSourceDto>> GetSourcesAsync();
}