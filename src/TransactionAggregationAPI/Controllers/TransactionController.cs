using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

/// <summary>
/// Provides endpoints for querying transactions and managing ingestion workflows.
/// </summary>
[ApiController]
[Route("api/transactions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IIngestionService _ingestionService;
    private readonly IUserRoleAccessor _userRoleAccessor;

    public TransactionController(
        ITransactionService transactionService,
        IIngestionService ingestionService,
        IUserRoleAccessor userRoleAccessor)
    {
        _transactionService = transactionService;
        _ingestionService = ingestionService;
        _userRoleAccessor = userRoleAccessor;
    }

    /// <summary>
    /// Retrieves a paginated list of transactions using the supplied filters.
    /// </summary>
    /// <param name="filter">The filtering and pagination options for the transaction query.</param>
    /// <returns>A paged transaction result set.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var result = await _transactionService.GetTransactionsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Gets a transaction summary grouped by the requested filters.
    /// </summary>
    /// <param name="filter">The filters to apply to the summary calculation.</param>
    /// <returns>A summary object describing the matching transactions.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary([FromQuery] TransactionFilterDto filter)
    {
        var summary = await _transactionService.GetTransactionSummaryAsync(filter);
        return Ok(summary);
    }

    /// <summary>
    /// Retrieves aggregated transaction data for reporting purposes.
    /// </summary>
    /// <param name="filter">The aggregation and pagination filters.</param>
    /// <returns>Aggregated transaction results.</returns>
    [HttpGet("aggregated")]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAggregated([FromQuery] TransactionFilterDto filter)
    {
        var result = await _transactionService.GetAggregatedTransactionsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single transaction by its unique identifier.
    /// </summary>
    /// <param name="transactionId">The unique transaction identifier.</param>
    /// <returns>The matching transaction if found.</returns>
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid transactionId)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
        return Ok(transaction);
    }

    /// <summary>
    /// Updates the category for a transaction. Restricted to administrators.
    /// </summary>
    /// <param name="transactionId">The transaction to update.</param>
    /// <param name="request">The updated category request.</param>
    /// <returns>The updated transaction.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{transactionId:guid}/category")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCategory(Guid transactionId, [FromBody] UpdateCategoryDto request)
    {
        var userRole = _userRoleAccessor.GetCurrentUserRole();
        var result = await _transactionService.UpdateCategoryAsync(transactionId, request.Category, userRole);
        return Ok(result);
    }

    /// <summary>
    /// Triggers ingestion from all configured transaction sources. Restricted to administrators.
    /// </summary>
    /// <returns>Ingestion results for all configured sources.</returns>
    [HttpPost("ingest")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> IngestTransactions()
    {
        var result = await _ingestionService.IngestAllSourcesAsync();
        return Ok(result);
    }
}