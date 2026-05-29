using System;
using System.Threading;
using System.Threading.Tasks;
using WexTran.Api.DTOs;
using WexTran.Api.Entities;
using WexTran.Api.Exceptions;
using WexTran.Api.External;
using WexTran.Api.Repositories;

namespace WexTran.Api.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateAsync(CreateTransactionRequest request);
        Task<TransactionByCurrencyResponse> GetByCurrencyAsync(Guid id, string currency);
    }

    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repository;
        private readonly IExchangeRateService _exchangeRateService;
        public TransactionService(ITransactionRepository repository, IExchangeRateService exchangeRateService)
        {
            _repository = repository;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var transaction = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                Description = request.Description?.Trim(),
                TransactionDate = request.TransactionDate,
                AmountUsd = Math.Round(request.AmountUsd, 2),
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(transaction);

            return new TransactionResponse
            {
                Id = transaction.Id,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                AmountUsd = transaction.AmountUsd
            };
        }

        public async Task<TransactionByCurrencyResponse> GetByCurrencyAsync(Guid id, string currency)
        {
            var transaction = await _repository.GetByIdAsync(id);
            if (transaction == null)
                throw new TransactionNotFoundException(id);

            var rateResult = await _exchangeRateService.GetExchangeRateAsync(currency, transaction.TransactionDate);
            if (rateResult == null)
                throw new CurrencyConversionUnavailableException(currency, transaction.TransactionDate);

            var convertedAmount = Math.Round(transaction.AmountUsd * rateResult.Rate, 2);

            return new TransactionByCurrencyResponse
            {
                Id = transaction.Id,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                AmountUsd = transaction.AmountUsd,
                Currency = rateResult.Currency,
                ExchangeRate = rateResult.Rate,
                ConvertedAmount = convertedAmount
            };
        }
    }
}
