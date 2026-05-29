using FluentAssertions;
using Moq;
using WexTran.Api.DTOs;
using WexTran.Api.Entities;
using WexTran.Api.Exceptions;
using WexTran.Api.External;
using WexTran.Api.Repositories;
using WexTran.Api.Services;

namespace WexTran.Tests.Unit;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repoMock = new();
    private readonly Mock<IExchangeRateService> _exchangeRateMock = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(_repoMock.Object, _exchangeRateMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsResponseWithAssignedId()
    {
        var request = new CreateTransactionRequest
        {
            Description = "Coffee",
            TransactionDate = new DateTime(2024, 3, 15),
            AmountUsd = 10.00m
        };

        var result = await _sut.CreateAsync(request);

        result.Id.Should().NotBeEmpty();
        result.Description.Should().Be("Coffee");
        result.TransactionDate.Should().Be(new DateTime(2024, 3, 15));
        result.AmountUsd.Should().Be(10.00m);
    }

    [Fact]
    public async Task CreateAsync_PersistsTransactionViaRepository()
    {
        var request = new CreateTransactionRequest
        {
            Description = "Hotel",
            TransactionDate = DateTime.Today,
            AmountUsd = 200m
        };

        await _sut.CreateAsync(request);

        _repoMock.Verify(r => r.AddAsync(It.Is<PurchaseTransaction>(t =>
            t.Description == "Hotel" && t.AmountUsd == 200m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_TrimsDescription()
    {
        var request = new CreateTransactionRequest
        {
            Description = "  groceries  ",
            TransactionDate = DateTime.Today,
            AmountUsd = 10m
        };

        var result = await _sut.CreateAsync(request);

        result.Description.Should().Be("groceries");
    }

    [Theory]
    [InlineData(2.005, 2.01)]   // midpoint rounds away from zero
    [InlineData(2.004, 2.00)]   // below midpoint rounds down
    [InlineData(2.995, 3.00)]   // higher midpoint rounds up
    [InlineData(10.10, 10.10)]  // already 2 dp, unchanged
    public async Task CreateAsync_RoundsAmountToNearestCent(decimal input, decimal expected)
    {
        var request = new CreateTransactionRequest
        {
            Description = "Test",
            TransactionDate = DateTime.Today,
            AmountUsd = input
        };

        var result = await _sut.CreateAsync(request);

        result.AmountUsd.Should().Be(expected);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.CreateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetByCurrencyAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByCurrencyAsync_ValidRequest_ReturnsConvertedAmount()
    {
        var id = Guid.NewGuid();
        var txn = new PurchaseTransaction
        {
            Id = id,
            Description = "Dinner",
            TransactionDate = new DateTime(2024, 3, 15),
            AmountUsd = 10.00m
        };
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(txn);
        _exchangeRateMock
            .Setup(e => e.GetExchangeRateAsync("Canada-Dollar", txn.TransactionDate))
            .ReturnsAsync(new ExchangeRateResult
            {
                Rate = 1.35m,
                RecordDate = new DateTime(2024, 3, 1),
                Currency = "Canada-Dollar"
            });

        var result = await _sut.GetByCurrencyAsync(id, "Canada-Dollar");

        result.Id.Should().Be(id);
        result.AmountUsd.Should().Be(10.00m);
        result.ExchangeRate.Should().Be(1.35m);
        result.ConvertedAmount.Should().Be(13.50m);
        result.Currency.Should().Be("Canada-Dollar");
    }

    [Theory]
    [InlineData(10.00, 1.355, 13.55)]  // standard multiplication
    [InlineData(1.00,  1.005, 1.01)]   // midpoint rounds away from zero
    [InlineData(1.00,  1.004, 1.00)]   // below midpoint rounds down
    public async Task GetByCurrencyAsync_RoundsConvertedAmountToNearestCent(
        decimal amount, decimal rate, decimal expected)
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new PurchaseTransaction { Id = id, TransactionDate = DateTime.Today, AmountUsd = amount });
        _exchangeRateMock
            .Setup(e => e.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new ExchangeRateResult { Rate = rate, RecordDate = DateTime.Today, Currency = "Test" });

        var result = await _sut.GetByCurrencyAsync(id, "Test");

        result.ConvertedAmount.Should().Be(expected);
    }

    [Fact]
    public async Task GetByCurrencyAsync_UnknownId_ThrowsTransactionNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PurchaseTransaction)null!);

        var act = () => _sut.GetByCurrencyAsync(Guid.NewGuid(), "Canada-Dollar");

        await act.Should().ThrowAsync<TransactionNotFoundException>();
    }

    [Fact]
    public async Task GetByCurrencyAsync_NoExchangeRate_ThrowsCurrencyConversionUnavailableException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new PurchaseTransaction { Id = id, TransactionDate = DateTime.Today, AmountUsd = 10m });
        _exchangeRateMock
            .Setup(e => e.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync((ExchangeRateResult)null!);

        var act = () => _sut.GetByCurrencyAsync(id, "Atlantis-Shell");

        await act.Should().ThrowAsync<CurrencyConversionUnavailableException>();
    }
}
