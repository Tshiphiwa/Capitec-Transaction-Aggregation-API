using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capitec_Transaction_Aggregation_API.Controllers.MockSources;

[ApiController]
[Route("api/mock-sources")]
public class WalletTransactionsController : MockSourceControllerBase
{
    private readonly IWalletTransactionsService _walletTransactionsService;

    public WalletTransactionsController(IWalletTransactionsService walletTransactionsService)
    {
        _walletTransactionsService = walletTransactionsService;
    }

    [HttpGet("wallet")]
    public ActionResult<IReadOnlyList<RawTransactionDto>> GetWalletTransactions()
    {
        return GetTransactions(_walletTransactionsService.GetTransactions());
    }
}
