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
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly ISourceService _sourceService;

    public SourcesController(ISourceService sourceService)
    {
        _sourceService = sourceService;
    }

    /// <summary>
    /// Returns all transaction sources with their status and transaction counts
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSources()
    {
        var sources = await _sourceService.GetSourcesAsync();
        return Ok(sources);
    }
}