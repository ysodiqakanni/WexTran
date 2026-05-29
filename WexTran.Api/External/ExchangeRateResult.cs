using System;

namespace WexTran.Api.External
{
    public class ExchangeRateResult
    {
        public decimal Rate { get; set; }
        public DateTime RecordDate { get; set; }
        public string Currency { get; set; }
    }
}
