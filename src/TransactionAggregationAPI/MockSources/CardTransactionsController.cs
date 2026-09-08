using Capitec_Transaction_Aggregation_API.MockSources.Data;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources/card")]
[ApiExplorerSettings(IgnoreApi = true)]
public class CardTransactionsController : ControllerBase
{

    [HttpGet("transactions")]
    public ActionResult GetCardTransactions() => Ok(CardTransactionData.Get());
}