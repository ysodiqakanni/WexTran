using System;

namespace WexTran.Api.Exceptions
{
    public class CurrencyConversionUnavailableException : Exception
    {
        public CurrencyConversionUnavailableException(string currency, DateTime purchaseDate)
            : base($"The purchase cannot be converted to the target currency.") { }
    }
}
