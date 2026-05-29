using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WexTran.Api.DTOs;
using WexTran.Api.External;

namespace WexTran.Tests.Integration;

public class TransactionsEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public TransactionsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    public Task InitializeAsync()
    {
        _factory.ExchangeRateServiceMock.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── POST /api/transactions ───────────────────────────────────────────────

    [Fact]
    public async Task Post_ValidRequest_Returns200WithAssignedId()
    {
        var body = new { description = "Coffee in Paris", transactionDate = "2024-03-15", amountUsd = 5.50 };

        var response = await _client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await Deserialize<TransactionResponse>(response);
        result.Id.Should().NotBeEmpty();
        result.Description.Should().Be("Coffee in Paris");
        result.AmountUsd.Should().Be(5.50m);
        result.TransactionDate.Should().Be(new DateTime(2024, 3, 15));
    }

    [Fact]
    public async Task Post_DescriptionExceeds50Characters_Returns400()
    {
        var body = new { description = new string('x', 51), transactionDate = "2024-03-15", amountUsd = 10.00 };

        var response = await _client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ZeroAmount_Returns400()
    {
        var body = new { description = "Free item", transactionDate = "2024-03-15", amountUsd = 0.00 };

        var response = await _client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NegativeAmount_Returns400()
    {
        var body = new { description = "Refund", transactionDate = "2024-03-15", amountUsd = -5.00 };

        var response = await _client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MissingDescription_Returns400()
    {
        var body = new { transactionDate = "2024-03-15", amountUsd = 10.00 };

        var response = await _client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/transactions/{id} ───────────────────────────────────────────

    [Fact]
    public async Task Get_ValidTransactionAndCurrency_Returns200WithConvertedAmount()
    {
        var created = await CreateTransaction("Lunch", "2024-03-15", 10.00m);

        _factory.ExchangeRateServiceMock
            .Setup(s => s.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15)))
            .ReturnsAsync(new ExchangeRateResult
            {
                Rate = 1.35m,
                RecordDate = new DateTime(2024, 3, 1),
                Currency = "Canada-Dollar"
            });

        var response = await _client.GetAsync($"/api/transactions/{created.Id}?currency=Canada-Dollar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await Deserialize<TransactionByCurrencyResponse>(response);
        result.Id.Should().Be(created.Id);
        result.AmountUsd.Should().Be(10.00m);
        result.ExchangeRate.Should().Be(1.35m);
        result.ConvertedAmount.Should().Be(13.50m);
        result.Currency.Should().Be("Canada-Dollar");
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}?currency=Canada-Dollar");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_NoExchangeRateAvailableWithinSixMonths_Returns422()
    {
        var created = await CreateTransaction("Hotel", "2024-03-15", 100.00m);

        _factory.ExchangeRateServiceMock
            .Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync((ExchangeRateResult)null!);

        var response = await _client.GetAsync($"/api/transactions/{created.Id}?currency=Atlantis-Shell");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_MissingCurrencyQueryParam_Returns400()
    {
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── authentication ───────────────────────────────────────────────────────

    [Fact]
    public async Task Post_MissingApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        var body = new { description = "Test", transactionDate = "2024-03-15", amountUsd = 10.00 };

        var response = await client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WrongApiKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        var body = new { description = "Test", transactionDate = "2024-03-15", amountUsd = 10.00 };

        var response = await client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<TransactionResponse> CreateTransaction(
        string description, string date, decimal amount)
    {
        var body = new { description, transactionDate = date, amountUsd = amount };
        var response = await _client.PostAsJsonAsync("/api/transactions", body);
        response.EnsureSuccessStatusCode();
        return (await Deserialize<TransactionResponse>(response))!;
    }

    private static async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }
}
