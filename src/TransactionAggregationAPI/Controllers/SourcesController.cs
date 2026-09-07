using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

[ApiController]
[Route("api/sources")]
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public SourcesController(ITransactionService transactionService)
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