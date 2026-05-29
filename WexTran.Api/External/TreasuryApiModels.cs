using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WexTran.Api.External
{

    public class TreasuryApiResponse
    {
        [JsonPropertyName("data")]
        public List<TreasuryRateRecord> Data { get; set; }
    }

    public class TreasuryRateRecord
    {
        [JsonPropertyName("exchange_rate")]
        public string ExchangeRate { get; set; }

        [JsonPropertyName("record_date")]
        public string RecordDate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}
