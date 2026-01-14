using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Net.Http;
using System.Text.Json;

namespace CurrencyExchangeAPI.Infrastructure
{
    public partial class CurrencyInfoServiceOXR : ExternalAPIServiceBase
    {
        public CurrencyInfoServiceOXR(HttpClient client, IConfiguration conf, ILogger<CurrencyInfoServiceOXR> logger) : base(client, conf, logger)
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
            
            LogCurrenciesAPI();

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

        [LoggerMessage(50, LogLevel.Information, "Obtained a list of all possible currencies from external API call")]
        partial void LogCurrenciesAPI();
    }
}
