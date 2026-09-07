using Capitec_Transaction_Aggregation_API.DTOs;

namespace Capitec_Transaction_Aggregation_API.Services;

public interface IRawTransactionSource
{
    IReadOnlyList<RawTransactionDto> GetTransactions();
}
