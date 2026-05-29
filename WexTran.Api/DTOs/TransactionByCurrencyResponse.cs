using System;

namespace WexTran.Api.DTOs
{
    public class TransactionByCurrencyResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal AmountUsd { get; set; }
        public string Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal ConvertedAmount { get; set; }
    }
}
