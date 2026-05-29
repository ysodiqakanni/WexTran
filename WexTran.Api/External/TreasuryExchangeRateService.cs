using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WexTran.Api.External
{
    public interface IExchangeRateService
    {
        Task<ExchangeRateResult> GetExchangeRateAsync(string currency, DateTime purchaseDate);
    }

    public class TreasuryExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;

        public TreasuryExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExchangeRateResult> GetExchangeRateAsync(string currency, DateTime purchaseDate)
        {
            var sixMonthsAgo = purchaseDate.AddMonths(-6);

            var url = "?fields=exchange_rate,record_date,currency" +
                      $"&filter=country_currency_desc:eq:{Uri.EscapeDataString(currency)}" +
                      $",record_date:lte:{purchaseDate:yyyy-MM-dd}" +
                      $",record_date:gte:{sixMonthsAgo:yyyy-MM-dd}" +
                      "&sort=-record_date" +
                      "&page[size]=1";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<TreasuryApiResponse>(content);

            var record = apiResponse?.Data.FirstOrDefault();
            if (record is null) return null;

            if (!decimal.TryParse(record.ExchangeRate, out var rate)) return null;
            if (!DateTime.TryParse(record.RecordDate, out var recordDate)) return null;

            return new ExchangeRateResult
            {
                Currency = record.Currency,
                Rate = rate,
                RecordDate = recordDate
            };
        }
    }
}
