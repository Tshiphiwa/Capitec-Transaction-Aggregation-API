using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

/// <summary>
/// Provides endpoints to view configured transaction sources.
/// </summary>
[ApiController]
[Route("api/sources")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SourcesController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public SourcesController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Retrieves the list of configured transaction sources.
    /// </summary>
    /// <returns>A collection of active source definitions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSources()
    {
        var sources = await _transactionService.GetSourcesAsync();
        return Ok(sources);
    }
}