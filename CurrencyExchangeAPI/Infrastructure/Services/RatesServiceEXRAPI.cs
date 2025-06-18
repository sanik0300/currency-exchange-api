using CurrencyExchangeAPI.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CurrencyExchangeAPI.Infrastructure
{
    public class RatesServiceEXRAPI : ExternalAPIServiceBase
    {
        public RatesServiceEXRAPI(HttpClient client, IConfiguration conf) : base(client, conf)
        {
            apiKeyConfPath = "API:ExchangeRate-API:Key";
            baseUrlConfPath = "API:ExchangeRate-API:BasePath";

            ProcessConfigurationFile(conf);

            client.BaseAddress = new Uri($"{_baseUrl}{_apiKey}/");
            serializerOptions.Converters.Insert(0, new ExchangeRatesJsonConverter());
        }

        public async Task<List<Exchange>> GetRatesFor(string code, IEnumerable<Currency> exclusive)
        {
            string url = $"latest/{code}";

            HttpResponseMessage responseMessage = await _httpClient.GetAsync(url);

            if(responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException($"Currency code {code} is unknown!");
            }

            DateTime savedGetTime = DateTime.Now;

            JsonNode resultNode;
            using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                resultNode = await JsonObject.ParseAsync(contentStream);
            }

            JsonNode objWithRates = resultNode["conversion_rates"];
            List<Exchange> onlineExchanges = objWithRates.AsObject().Deserialize<List<Exchange>>(serializerOptions);

            List<Exchange> exclusiveExchanges = onlineExchanges.Where(oex => exclusive.Any(c => c.Code == oex.PriceCode)).ToList();
            foreach(Exchange ex in exclusiveExchanges)
            {
                ex.BaseCode = code;
                ex.MeasuredAt = savedGetTime;
            }
            return exclusiveExchanges;
        }
    }
}
