using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Net.Http;
using System.Text.Json;

namespace CurrencyExchangeAPI.Infrastructure
{
    public class CurrencyInfoServiceOXR : ExternalAPIServiceBase
    {
        public CurrencyInfoServiceOXR(HttpClient client, IConfiguration conf) : base(client, conf)
        {
            apiKeyConfPath = "API:OpenExchangeRates:Key";
            baseUrlConfPath = "API:OpenExchangeRates:BasePath";

            ProcessConfigurationFile(conf);

            _httpClient.BaseAddress = new Uri(_baseUrl);
            serializerOptions.Converters.Insert(0, new CurrencyListJsonConverter());    
        } 

        public async Task<Currency> GetNewCurrency(string code)
        {
            string url = $"currencies.json?app_id={_apiKey}";

            HttpResponseMessage responseMessage = await _httpClient.GetAsync(url);

            List<Currency> currenciesOnline;
            using (Stream content = await responseMessage.Content.ReadAsStreamAsync())
            {
                currenciesOnline = await JsonSerializer.DeserializeAsync<List<Currency>>(content, serializerOptions);
            }

            Currency? result = currenciesOnline.FirstOrDefault(c => c.Code == code);

            if(result == null)
            {
                throw new KeyNotFoundException($"Currency code {code} is unknown!");
            }

            return result;
        }
    }
}
