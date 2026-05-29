using System;

namespace WexTran.Api.Exceptions
{
    public class CurrencyConversionUnavailableException : Exception
    {
        public CurrencyConversionUnavailableException(string currency, DateTime purchaseDate)
            : base($"No exchange rate available for '{currency}' within 6 months of {purchaseDate:yyyy-MM-dd}.") { }
    }
}
