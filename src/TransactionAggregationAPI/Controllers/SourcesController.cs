using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

[ApiController]
[Route("api/sources")]
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly TransactionService _transactionService;

    public SourcesController(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSources()
    {
        var sources = await _transactionService.GetSourcesAsync();
        return Ok(sources);
    }
}