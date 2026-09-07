using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

public abstract class MockSourceControllerBase : ControllerBase
{
    protected ActionResult<IReadOnlyList<RawTransactionDto>> GetTransactions(IReadOnlyList<RawTransactionDto> transactions)
    {
        return Ok(transactions);
    }
}
