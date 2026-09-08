using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Categorization;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace TransactionAggregationAPI.Tests;

public class IngestionServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;

    public IngestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"IngestionServiceTests-{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // ── IngestSourceAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task IngestSourceAsync_WhenSourceReturnsTransactions_IngestsAndCategorizesViaKeyword()
    {
        var source = await SeedSourceAsync("Telecom", "TELCO");
        var service = CreateService(StubFactory(OneTransactionJson("txn-1", "Vodacom recharge", "9999")));

        var result = await service.IngestSourceAsync(source);

        result.Success.Should().BeTrue();
        result.IngestedCount.Should().Be(1);
        var saved = await _dbContext.Transactions.SingleAsync();
        saved.Category.Should().Be("Utilities");
        saved.CategorySource.Should().Be(CategorySource.Keyword);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenSourceReturnsTransactionWithKnownMcc_CategorizesViaMcc()
    {
        var source = await SeedSourceAsync();
        var service = CreateService(StubFactory(OneTransactionJson("txn-mcc", "Supermarket", "5411")));

        await service.IngestSourceAsync(source);

        var saved = await _dbContext.Transactions.SingleAsync();
        saved.Category.Should().Be("Groceries");
        saved.CategorySource.Should().Be(CategorySource.MccCode);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenDuplicateReferenceExists_SkipsExistingTransaction()
    {
        var source = await SeedSourceAsync();
        _dbContext.Transactions.Add(BuildTransaction(source, "dup-ref"));
        await _dbContext.SaveChangesAsync();

        var service = CreateService(StubFactory(OneTransactionJson("dup-ref", "Groceries", "5411")));

        var result = await service.IngestSourceAsync(source);

        result.IngestedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        (await _dbContext.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenMultipleTransactionsReturned_IngestsAllNew()
    {
        var source = await SeedSourceAsync();
        var json = $"[{RawTxnJson("ref-a", "Groceries", "5411")},{RawTxnJson("ref-b", "Dining", "5812")}]";
        var service = CreateService(StubFactory(json));

        var result = await service.IngestSourceAsync(source);

        result.IngestedCount.Should().Be(2);
        (await _dbContext.Transactions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenExternalSourceReturnsServerError_ThrowsHttpRequestException()
    {
        var source = await SeedSourceAsync();
        var service = CreateService(StubFactory(System.Net.HttpStatusCode.InternalServerError));

        var act = async () => await service.IngestSourceAsync(source);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task IngestSourceAsync_WhenResponseBodyIsMalformedJson_ThrowsJsonException()
    {
        var source = await SeedSourceAsync();
        var service = CreateService(StubFactory("not-valid-json"));

        var act = async () => await service.IngestSourceAsync(source);

        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task IngestSourceAsync_WhenExternalSourceIsUnavailable_ThrowsHttpRequestException()
    {
        var source = await SeedSourceAsync();
        var service = CreateService(StubFactory(throwOnRequest: true));

        var act = async () => await service.IngestSourceAsync(source);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── IngestAllSourcesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IngestAllSourcesAsync_WhenAllSourcesSucceed_ReturnsTotalIngestedCount()
    {
        var sourceA = await SeedSourceAsync("Bank A", "BANKA");
        var sourceB = await SeedSourceAsync("Bank B", "BANKB");

        // Each source gets a unique reference so neither is treated as a duplicate
        var callCount = 0;
        var service = CreateService(new CallCountingStubFactory(() =>
        {
            callCount++;
            var json = callCount == 1
                ? $"[{RawTxnJson("ref-a", "Groceries", "5411")}]"
                : $"[{RawTxnJson("ref-b", "Dining", "5812")}]";
            return new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent(json) };
        }));

        var result = await service.IngestAllSourcesAsync();

        result.TotalIngested.Should().Be(2);
        result.SourceResults.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task IngestAllSourcesAsync_WhenOneSourceFails_ContinuesAndReportsFailure()
    {
        await SeedSourceAsync("Good Source", "GOOD");
        await SeedSourceAsync("Bad Source", "BAD");

        var callCount = 0;
        var service = CreateService(new CallCountingStubFactory(() =>
        {
            callCount++;
            return callCount == 1
                ? new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent($"[{RawTxnJson("ref-ok", "Groceries", "5411")}]") }
                : new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError };
        }));

        var result = await service.IngestAllSourcesAsync();

        result.SourceResults.Should().HaveCount(2);
        result.SourceResults.Should().ContainSingle(r => r.Success && r.IngestedCount == 1);
        result.SourceResults.Should().ContainSingle(r => !r.Success);
    }

    [Fact]
    public async Task IngestAllSourcesAsync_WhenNoActiveSources_ReturnsZeroIngested()
    {
        var service = CreateService(StubFactory("[]"));

        var result = await service.IngestAllSourcesAsync();

        result.TotalIngested.Should().Be(0);
        result.SourceResults.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<TransactionSource> SeedSourceAsync(string name = "Bank", string code = "BANK")
    {
        var source = new TransactionSource { Id = Guid.NewGuid(), Name = name, Code = code, BaseUrl = "https://example.com", IsActive = true };
        _dbContext.TransactionSources.Add(source);
        await _dbContext.SaveChangesAsync();
        return source;
    }

    private static Transaction BuildTransaction(TransactionSource source, string reference) => new()
    {
        Id = Guid.NewGuid(),
        Amount = 100m,
        Currency = "ZAR",
        Description = "Groceries",
        MerchantName = "Store",
        MccCode = "5411",
        Category = "Groceries",
        CategorySource = CategorySource.MccCode,
        TransactionType = TransactionType.CardSwipe,
        Direction = TransactionDirection.Debit,
        TransactionDate = DateTime.UtcNow,
        Reference = reference,
        FromAccount = "A",
        ToAccount = "B",
        SourceId = source.Id,
        Source = source
    };

    private IngestionService CreateService(IHttpClientFactory factory) =>
        new(_dbContext,
            new CategorizationService(new ICategorizationStrategy[] { new MccCategorizationStrategy(), new KeywordCategorizationStrategy() }),
            factory,
            new TransactionMapper(),
            NullLogger<IngestionService>.Instance);

    private static string OneTransactionJson(string reference, string description, string mcc) =>
        $"[{RawTxnJson(reference, description, mcc)}]";

    private static string RawTxnJson(string reference, string description, string mcc) =>
        $"{{\"reference\":\"{reference}\",\"amount\":150.00,\"currency\":\"ZAR\",\"description\":\"{description}\",\"mccCode\":\"{mcc}\",\"transactionDate\":\"2024-01-01T12:00:00Z\",\"merchantName\":\"Store\",\"transactionType\":\"CARD_SWIPE\",\"direction\":\"DEBIT\",\"fromAccount\":\"A\",\"toAccount\":\"B\"}}";

    private static IHttpClientFactory StubFactory(string json) => new StubHttpClientFactory(json);
    private static IHttpClientFactory StubFactory(System.Net.HttpStatusCode code) => new StubHttpClientFactory(code);
    private static IHttpClientFactory StubFactory(bool throwOnRequest) => new StubHttpClientFactory(throwOnRequest);

    // ── Stub infrastructure ───────────────────────────────────────────────────

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string _json;
        private readonly System.Net.HttpStatusCode _statusCode;
        private readonly bool _throwOnRequest;

        public StubHttpClientFactory(string json) { _json = json; _statusCode = System.Net.HttpStatusCode.OK; }
        public StubHttpClientFactory(System.Net.HttpStatusCode code) { _json = string.Empty; _statusCode = code; }
        public StubHttpClientFactory(bool throwOnRequest) { _json = string.Empty; _statusCode = System.Net.HttpStatusCode.OK; _throwOnRequest = throwOnRequest; }

        public HttpClient CreateClient(string name) =>
            new(new StubHttpMessageHandler(_json, _statusCode, _throwOnRequest));
    }

    private sealed class CallCountingStubFactory : IHttpClientFactory
    {
        private readonly Func<HttpResponseMessage> _responseFactory;
        public CallCountingStubFactory(Func<HttpResponseMessage> responseFactory) => _responseFactory = responseFactory;
        public HttpClient CreateClient(string name) => new(new DelegatingStubHandler(_responseFactory));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly System.Net.HttpStatusCode _statusCode;
        private readonly bool _throwOnRequest;

        public StubHttpMessageHandler(string json, System.Net.HttpStatusCode statusCode, bool throwOnRequest)
        { _json = json; _statusCode = statusCode; _throwOnRequest = throwOnRequest; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throwOnRequest) throw new HttpRequestException("Upstream unavailable.");
            return Task.FromResult(new HttpResponseMessage { StatusCode = _statusCode, Content = new StringContent(_json) });
        }
    }

    private sealed class DelegatingStubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _factory;
        public DelegatingStubHandler(Func<HttpResponseMessage> factory) => _factory = factory;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_factory());
    }
}
