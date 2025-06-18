using System;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CurrencyExchangeApiTest.Integration
{
    public sealed class EXRAPI_ClientTest : ExternalApiTestBase
    {
        private const string errorResultStr = "error",
                             errorMessage = "unsupported-code",
                             successMessage = "success";
    
        public EXRAPI_ClientTest()
        {
            ReadConfigLines("test-config-rates.txt");
            httpClient.BaseAddress = new Uri($"{basicUrl}{apiKey}/");
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("UAH")]
        [InlineData("JPY")]
        [InlineData("RON")]
        public async Task ExistingCurrencyRate(string currencyCode)
        {
            string url = $"latest/{currencyCode}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);

            JsonNode resultNode;

            using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                resultNode = await JsonObject.ParseAsync(contentStream);
            }

            string updateString = resultNode["time_last_update_utc"].GetValue<string>();
            DateTime updateDT = DateTime.Parse(updateString, CultureInfo.InvariantCulture);
            double daysDiff = (DateTime.Now - updateDT).TotalDays;
            Assert.True(responseMessage.IsSuccessStatusCode);
            Assert.NotNull(responseMessage.Content.Headers.ContentType);
            Assert.Equal(contentTypeStr, responseMessage.Content.Headers.ContentType.MediaType);

            Assert.Equal(successMessage, resultNode["result"].GetValue<string>());
            Assert.Equal(currencyCode, resultNode["base_code"].GetValue<string>());

            Assert.True(daysDiff<1);
            Assert.True(resultNode["conversion_rates"].AsObject().Count > 1);
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("XZZ")]
        public async Task NonExistingCurrencyRateError(string code)
        {
            string url = $"latest/{code}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);

            JsonNode resultNode;

            using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                resultNode = await JsonObject.ParseAsync(contentStream);
            }

            Assert.Equal(HttpStatusCode.NotFound, responseMessage.StatusCode);
            Assert.Equal(errorResultStr, resultNode["result"].GetValue<string>());
            Assert.Equal(errorMessage, resultNode["error-type"].GetValue<string>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("12345")]
        public async Task InvalidCodeRateError(string code)
        {
            string url = $"latest/{code}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);

            Assert.Equal(HttpStatusCode.NotFound, responseMessage.StatusCode);
            Assert.NotEqual(contentTypeStr, responseMessage.Content.Headers.ContentType.MediaType);
        }

    }
}
