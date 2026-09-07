using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources")]
public class CardTransactionsController : MockSourceControllerBase
{
    private readonly ICardTransactionsService _cardTransactionsService;

    public CardTransactionsController(ICardTransactionsService cardTransactionsService)
    {
        _cardTransactionsService = cardTransactionsService;
    }

    [HttpGet("card")]
    public ActionResult<IReadOnlyList<RawTransactionDto>> GetCardTransactions()
    {
        return GetTransactions(_cardTransactionsService.GetTransactions());
    }
}
