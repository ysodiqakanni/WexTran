using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace WexTran.Api.External
{
    public class CachedExchangeRateService : IExchangeRateService
    {
        private readonly IExchangeRateService _inner;
        private readonly IMemoryCache _cache;

        public CachedExchangeRateService(IExchangeRateService inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<ExchangeRateResult> GetExchangeRateAsync(string currency, DateTime purchaseDate)
        {
            var key = $"exchange_rate:{currency}:{purchaseDate:yyyy-MM-dd}";

            if (_cache.TryGetValue(key, out ExchangeRateResult cached))
                return cached;

            var result = await _inner.GetExchangeRateAsync(currency, purchaseDate);

            if (result != null)
                _cache.Set(key, result, TimeSpan.FromHours(24));

            return result;
        }
    }
}
