using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchangeApiTest.Integration
{
    public abstract class ExternalApiTestBase : IDisposable
    {
        protected const string apiCredentialKey = "API_KEY",
                               baseUrlKey = "BASE_URL",
                               contentTypeStr = "application/json";

        protected readonly HttpClient httpClient;

        protected string apiKey=default!, 
                         basicUrl=default!;

        protected void ReadConfigLines(string configFileName)
        {
            string[] configLines = File.ReadAllLines(configFileName);

            Dictionary<string, string> configPairs = configLines.Select(line =>
            {
                string[] parts = line.Split('=');
                return new KeyValuePair<string, string>(parts[0], parts[1]);
            }).ToDictionary();

            apiKey = configPairs[apiCredentialKey];
            basicUrl = configPairs[baseUrlKey];
        }

        protected ExternalApiTestBase() => httpClient = new HttpClient();

        public void Dispose()
        {
            httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
