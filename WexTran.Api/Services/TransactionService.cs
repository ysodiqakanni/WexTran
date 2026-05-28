using System;
using System.Threading;
using System.Threading.Tasks;
using WexTran.Api.DTOs;
using WexTran.Api.Entities;
using WexTran.Api.Repositories;

namespace WexTran.Api.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateAsync(CreateTransactionRequest request);
    }

    public class TransactionService
    {
        private readonly ITransactionRepository _repository;
        public TransactionService(ITransactionRepository repository)
        {
            _repository = repository;
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

    }
}
