using Capitec_Transaction_Aggregation_API.Infrastructure;
using Capitec_Transaction_Aggregation_API.Models;
using Capitec_Transaction_Aggregation_API.Services;
using Capitec_Transaction_Aggregation_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace TransactionAggregationAPI.Tests;

public class IngestionServiceTests
{
    [Fact]
    public async Task ImportTransactionsAsync_WhenTransactionDoesNotExist_PersistsTransaction()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(dbContext, new CategorizationService());
        var transactions = new[]
        {
            CreateTransaction(source, "seed-1", 99.99m, "Groceries")
        };

        // Act
        var imported = await service.ImportTransactionsAsync(transactions, source.Id);

        // Assert
        Assert.Equal(1, imported);
        Assert.Equal("Groceries", dbContext.Transactions.Single().Category);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenSourceReturnsTransactions_UsesKeywordCategoryAndIngestsResult()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource("Telecom", "TELCO");
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(
            dbContext,
            new CategorizationService(),
            NullLogger<IngestionService>.Instance,
            new StubHttpClientFactory("[{\"reference\":\"txn-1\",\"amount\":150.00,\"currency\":\"ZAR\",\"description\":\"Vodacom recharge\",\"mccCode\":\"9999\",\"transactionDate\":\"2024-01-01T12:00:00Z\",\"merchantName\":\"Vodacom\",\"transactionType\":\"CARD_SWIPE\",\"direction\":\"DEBIT\",\"fromAccount\":\"A\",\"toAccount\":\"B\"}]"));

        // Act
        var result = await service.IngestSourceAsync(source);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.IngestedCount);
        Assert.Equal(CategorySource.Keyword, dbContext.Transactions.Single().CategorySource);
        Assert.Equal("Utilities", dbContext.Transactions.Single().Category);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenExternalSourceReturnsServerError_ThrowsHttpRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource("Telecom", "TELCO");
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(
            dbContext,
            new CategorizationService(),
            NullLogger<IngestionService>.Instance,
            new StubHttpClientFactory(statusCode: System.Net.HttpStatusCode.InternalServerError));

        // Act
        var act = async () => await service.IngestSourceAsync(source);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Fact]
    public async Task ImportTransactionsAsync_WhenDuplicateReferenceExists_SkipsExistingTransaction()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource();
        dbContext.TransactionSources.Add(source);
        var existing = CreateTransaction(source, "duplicate-ref", 125m, "Groceries");
        dbContext.Transactions.Add(existing);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(dbContext, new CategorizationService());
        var duplicate = CreateTransaction(source, "duplicate-ref", 99m, "Transport");

        // Act
        var imported = await service.ImportTransactionsAsync(new[] { duplicate }, source.Id);

        // Assert
        Assert.Equal(0, imported);
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task IngestSourceAsync_WhenResponseBodyIsMalformedJson_ThrowsJsonException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource("Telecom", "TELCO");
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(
            dbContext,
            new CategorizationService(),
            NullLogger<IngestionService>.Instance,
            new StubHttpClientFactory("not-valid-json"));

        // Act
        var act = async () => await service.IngestSourceAsync(source);

        // Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(act);
    }

    [Fact]
    public async Task IngestSourceAsync_WhenExternalSourceIsUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var source = CreateSource("Telecom", "TELCO");
        dbContext.TransactionSources.Add(source);
        await dbContext.SaveChangesAsync();

        IIngestionService service = new IngestionService(
            dbContext,
            new CategorizationService(),
            NullLogger<IngestionService>.Instance,
            new StubHttpClientFactory(throwOnRequest: true));

        // Act
        var act = async () => await service.IngestSourceAsync(source);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static TransactionSource CreateSource(string name = "Bank", string code = "BANK")
    {
        return new TransactionSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            BaseUrl = "https://example.com"
        };
    }

    private static Transaction CreateTransaction(TransactionSource source, string reference, decimal amount, string description)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = "ZAR",
            Description = description,
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
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string _json;
        private readonly System.Net.HttpStatusCode _statusCode;
        private readonly bool _throwOnRequest;

        public StubHttpClientFactory(string json)
        {
            _json = json;
            _statusCode = System.Net.HttpStatusCode.OK;
            _throwOnRequest = false;
        }

        public StubHttpClientFactory(System.Net.HttpStatusCode statusCode)
        {
            _json = string.Empty;
            _statusCode = statusCode;
            _throwOnRequest = false;
        }

        public StubHttpClientFactory(bool throwOnRequest)
        {
            _json = string.Empty;
            _statusCode = System.Net.HttpStatusCode.OK;
            _throwOnRequest = throwOnRequest;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new StubHttpMessageHandler(_json, _statusCode, _throwOnRequest));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly System.Net.HttpStatusCode _statusCode;
        private readonly bool _throwOnRequest;

        public StubHttpMessageHandler(string json, System.Net.HttpStatusCode statusCode, bool throwOnRequest)
        {
            _json = json;
            _statusCode = statusCode;
            _throwOnRequest = throwOnRequest;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throwOnRequest)
            {
                throw new HttpRequestException("The upstream service is unavailable.");
            }

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_json)
            });
        }
    }
}
