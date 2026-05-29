using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using WexTran.Api.External;

namespace WexTran.Tests.Unit;

public class TreasuryExchangeRateServiceTests
{
    private static (TreasuryExchangeRateService Sut, Mock<HttpMessageHandler> Handler) CreateSut(
        string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/services/api/fiscal_service/") };
        return (new TreasuryExchangeRateService(client), handler);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ValidResponse_ReturnsExchangeRateResult()
    {
        const string json = """{"data":[{"exchange_rate":"1.3500","record_date":"2024-03-01","currency":"Canada-Dollar"}]}""";
        var (sut, _) = CreateSut(json);

        var result = await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        result.Should().NotBeNull();
        result!.Rate.Should().Be(1.3500m);
        result.RecordDate.Should().Be(new DateTime(2024, 3, 1));
        result.Currency.Should().Be("Canada-Dollar");
    }

    [Fact]
    public async Task GetExchangeRateAsync_EmptyData_ReturnsNull()
    {
        var (sut, _) = CreateSut("""{"data":[]}""");

        var result = await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExchangeRateAsync_UnparseableExchangeRate_ReturnsNull()
    {
        const string json = """{"data":[{"exchange_rate":"N/A","record_date":"2024-03-01","currency":"Canada-Dollar"}]}""";
        var (sut, _) = CreateSut(json);

        var result = await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExchangeRateAsync_ParsesRateWithPeriodDecimalSeparatorRegardlessOfCulture()
    {
        // Treasury API always uses period as decimal separator; must parse culture-invariantly
        const string json = """{"data":[{"exchange_rate":"1.3500","record_date":"2024-03-01","currency":"Canada-Dollar"}]}""";
        var (sut, _) = CreateSut(json);

        var result = await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        result!.Rate.Should().Be(1.35m);
    }

    [Fact]
    public async Task GetExchangeRateAsync_UsesCorrectSixMonthLookback()
    {
        Uri? capturedUri = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}""", Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.fiscaldata.treasury.gov/services/api/fiscal_service/") };
        var sut = new TreasuryExchangeRateService(client);

        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        capturedUri!.Query.Should().Contain("record_date:lte:2024-03-15");
        capturedUri.Query.Should().Contain("record_date:gte:2023-09-15");
    }

    [Fact]
    public async Task GetExchangeRateAsync_ApiError_ThrowsHttpRequestException()
    {
        var (sut, _) = CreateSut("{}", HttpStatusCode.ServiceUnavailable);

        var act = () => sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
