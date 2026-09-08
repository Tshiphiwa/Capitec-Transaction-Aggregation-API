using Capitec_Transaction_Aggregation_API.MockSources.Data;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources/eft")]
[ApiExplorerSettings(IgnoreApi = true)]
public class EftTransactionsController : ControllerBase
{
    [HttpGet("transactions")]
    public ActionResult GetTransactions() => Ok(EftTransactionData.Get());
}
