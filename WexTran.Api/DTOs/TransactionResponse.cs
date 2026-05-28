using System;

namespace WexTran.Api.DTOs
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal AmountUsd { get; set; }
    }
}
