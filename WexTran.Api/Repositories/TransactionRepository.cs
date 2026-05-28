using System;
using System.Threading;
using System.Threading.Tasks;
using WexTran.Api.Entities;

namespace WexTran.Api.Repositories
{
    public interface ITransactionRepository
    {
        Task<PurchaseTransaction> AddAsync(PurchaseTransaction transaction);
        Task<PurchaseTransaction> GetByIdAsync(Guid id);
    }

    public class TransactionRepository : ITransactionRepository
    {
        private readonly WexTransactionDbContext _context;

        public TransactionRepository(WexTransactionDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseTransaction> AddAsync(PurchaseTransaction transaction)
        {
            _context.PurchaseTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<PurchaseTransaction> GetByIdAsync(Guid id)
        {
            return await _context.PurchaseTransactions.FindAsync([id]);
        }
    }
}
