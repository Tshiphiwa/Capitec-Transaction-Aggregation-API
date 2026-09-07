using System.Text.Json;
using Capitec_Transaction_Aggregation_API.DTOs;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;

namespace Capitec_Transaction_Aggregation_API.Services;

public class TransactionSourceClient : ITransactionSourceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TransactionSourceClient> _logger;

    public TransactionSourceClient(IHttpClientFactory httpClientFactory, ILogger<TransactionSourceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RawTransactionDto>> GetTransactionsAsync(TransactionSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{source.BaseUrl.TrimEnd('/')}/api/transactions", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = JsonSerializer.Deserialize<List<RawTransactionDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is null)
        {
            _logger.LogWarning("Source {SourceCode} returned no transaction payload.", source.Code);
            return Array.Empty<RawTransactionDto>();
        }

        return result;
    }
}
