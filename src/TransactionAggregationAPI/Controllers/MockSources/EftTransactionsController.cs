using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources")]
public class EftTransactionsController : MockSourceControllerBase
{
    private readonly IEftTransactionsService _eftTransactionsService;

    public EftTransactionsController(IEftTransactionsService eftTransactionsService)
    {
        _eftTransactionsService = eftTransactionsService;
    }

    [HttpGet("eft")]
    public ActionResult<IReadOnlyList<RawTransactionDto>> GetEftTransactions()
    {
        return GetTransactions(_eftTransactionsService.GetTransactions());
    }
}
