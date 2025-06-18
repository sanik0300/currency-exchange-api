using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;

namespace CurrencyExchangeAPI.Infrastructure
{
    public abstract class ExternalAPIServiceBase
    {
        protected readonly JsonSerializerOptions serializerOptions;
        protected string apiKeyConfPath, baseUrlConfPath;

        protected string _apiKey { get; private set; }
        protected string _baseUrl { get; private set; }
        public HttpClient _httpClient { get; protected set; }

        protected void ProcessConfigurationFile(IConfiguration conf)
        {
            _apiKey = conf[apiKeyConfPath];
            _baseUrl = conf[baseUrlConfPath];
        }

        protected ExternalAPIServiceBase(HttpClient client, IConfiguration conf)
        {
            _httpClient = client;
            serializerOptions = new JsonSerializerOptions();
        }
    }
}
