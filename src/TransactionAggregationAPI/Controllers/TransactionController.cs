using System.Security.Claims;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IIngestionService _ingestionService;

    public TransactionController(ITransactionService transactionService, IIngestionService ingestionService)
    {
        _transactionService = transactionService;
        _ingestionService = ingestionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var result = await _transactionService.GetTransactionsAsync(filter);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] TransactionFilterDto filter)
    {
        var summary = await _transactionService.GetTransactionSummaryAsync(filter);
        return Ok(summary);
    }

 [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregated([FromQuery] TransactionFilterDto filter)
    {
        var result = await _transactionService.GetAggregatedTransactionsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{transactionId:guid}")]
    public async Task<IActionResult> GetTransactionById(Guid transactionId)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
        return Ok(transaction);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{transactionId:guid}/category")]
    public async Task<IActionResult> UpdateCategory(Guid transactionId, [FromBody] UpdateCategoryDto request)
    {
        var roleString = User.FindFirstValue(ClaimTypes.Role) ?? String.Empty;
        var userRole = Enum.TryParse<UserRole>(roleString, out var role)
            ? role
            : UserRole.Analyst;

        var result = await _transactionService.UpdateCategoryAsync(transactionId, request.Category, userRole);
        return Ok(result);
    }

    [HttpPost("ingest")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IngestTransactions()
    {
        var result = await _ingestionService.IngestAllSourcesAsync();
        return Ok(result);
    }
}