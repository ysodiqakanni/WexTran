using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using WexTran.Api.Entities;

namespace WexTran.Api.Repositories
{
    public class WexTransactionDbContext : DbContext
    {
        public WexTransactionDbContext(DbContextOptions<WexTransactionDbContext> options) : base(options) { }

        public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PurchaseTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AmountUsd).HasPrecision(18, 2);
            });
        }
    }
}
