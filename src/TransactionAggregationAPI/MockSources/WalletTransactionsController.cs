using Capitec_Transaction_Aggregation_API.MockSources.Data;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources/wallet")]
[ApiExplorerSettings(IgnoreApi = true)]
public class WalletTransactionsController : ControllerBase
{
    [HttpGet("transactions")]
    public ActionResult GetTransactions() => Ok(WalletTransactionData.Get());
}