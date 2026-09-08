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
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IIngestionService _ingestionService;

    public TransactionController(
        ITransactionService transactionService,
        IIngestionService ingestionService)
    {
        _transactionService = transactionService;
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Retrieves a paginated list of transactions using the supplied filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var transactions = await _transactionService.GetTransactionsAsync(filter);
        return Ok(transactions);
    }

    /// <summary>
    /// Gets a transaction summary (tot debits, tot credits, net amount, spend by category) grouped by the requested filters.
    /// </summary>
    // Must be defined before / {id} else "summary" is matched as a Guid and returns not found (400)
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionSummary([FromQuery] TransactionFilterDto filter)
    {
        var summary = await _transactionService.GetTransactionSummaryAsync(filter);
        return Ok(summary);
    }

    /// <summary>
    /// Retrieves aggregated transaction data for reporting purposes.
    /// </summary>
    [HttpGet("aggregated")]
    [ProducesResponseType(typeof(PagedResultDto<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAggregated([FromQuery] TransactionFilterDto filter)
    {
        var aggregatedResult = await _transactionService.GetAggregatedTransactionsAsync(filter);
        return Ok(aggregatedResult);
    }

    /// <summary>
    /// Returns a single transaction by its unique identifier (ID).
    /// </summary>
    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid transactionId)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
        return Ok(transaction);
    }

    /// <summary>
    /// Updates the category for a transaction. Restricted to administrators.
    /// </summary>
    [HttpPatch("{transactionId:guid}/category")]
    [Authorize(Roles = "Admin")] // Enforce role at HTTP layer
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCategory(Guid transactionId, [FromBody] UpdateCategoryDto request)
    {
        var category = await _transactionService.UpdateCategoryAsync(transactionId, request.Category);
        return Ok(category);
    }

    /// <summary>
    /// Triggers ingestion from all active transaction sources. Restricted to administrators.
    /// </summary>>
    [HttpPost("ingest")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> IngestTransactions()
    {
        var result = await _ingestionService.IngestAllSourcesAsync();
        return Ok(result);
    }
}