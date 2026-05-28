using System;
using System.ComponentModel.DataAnnotations;

namespace WexTran.Api.Entities
{
    public class PurchaseTransaction
    {
        [Key]
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal AmountUsd { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
