using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using WexTran.Api.External;

namespace WexTran.Tests.Unit;

public class CachedExchangeRateServiceTests
{
    private static (CachedExchangeRateService Sut, Mock<IExchangeRateService> Inner) Create()
    {
        var inner = new Mock<IExchangeRateService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new CachedExchangeRateService(inner.Object, cache), inner);
    }

    [Fact]
    public async Task GetExchangeRateAsync_CacheMiss_CallsInnerServiceAndReturnsResult()
    {
        var (sut, inner) = Create();
        var expected = new ExchangeRateResult { Rate = 1.35m, RecordDate = DateTime.Today, Currency = "Canada-Dollar" };
        inner.Setup(s => s.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15)))
            .ReturnsAsync(expected);

        var result = await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        result.Should().BeEquivalentTo(expected);
        inner.Verify(s => s.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15)), Times.Once);
    }

    [Fact]
    public async Task GetExchangeRateAsync_SecondCallSameCurrencyAndDate_ReturnsCachedResult()
    {
        var (sut, inner) = Create();
        inner.Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new ExchangeRateResult { Rate = 1.35m, RecordDate = DateTime.Today, Currency = "Canada-Dollar" });

        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));
        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        inner.Verify(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task GetExchangeRateAsync_DifferentCurrencyOrDate_EachIsASeparateCacheMiss()
    {
        var (sut, inner) = Create();
        inner.Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new ExchangeRateResult { Rate = 1.0m, RecordDate = DateTime.Today, Currency = "X" });

        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));
        await sut.GetExchangeRateAsync("Mexico-Peso",   new DateTime(2024, 3, 15));
        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 4, 15));

        inner.Verify(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetExchangeRateAsync_NullResult_IsNotCached()
    {
        var (sut, inner) = Create();
        inner.Setup(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync((ExchangeRateResult)null!);

        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));
        await sut.GetExchangeRateAsync("Canada-Dollar", new DateTime(2024, 3, 15));

        inner.Verify(s => s.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(2));
    }
}
