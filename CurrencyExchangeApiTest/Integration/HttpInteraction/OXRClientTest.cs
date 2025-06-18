using System.Net.Http;
using System.Text.Json;

namespace CurrencyExchangeApiTest.Integration
{
    public sealed class OXRClientTest : ExternalApiTestBase
    {
        public OXRClientTest()
        {
            ReadConfigLines("test-config-oxr.txt");
            httpClient.BaseAddress = new Uri(basicUrl);
        }

        [Fact]
        public async Task GetAllCurrenciesListTest()
        {
            string url = $"currencies.json?app_id={apiKey}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);

            Assert.True(responseMessage.IsSuccessStatusCode);

            Assert.NotNull(responseMessage.Content.Headers.ContentType);
            Assert.Equal(contentTypeStr, responseMessage.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("USD", "United States Dollar")]
        [InlineData("UAH", "Ukrainian Hryvnia")]
        [InlineData("PLN", "Polish Zloty")]
        [InlineData("INR", "Indian Rupee")]
        public async Task ExistingCurrenciesInListTest(string currencyCode, string currencyName)
        {
            string url = $"currencies.json?app_id={apiKey}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);

            Dictionary<string, string> currencies;

            using(Stream contentStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                currencies = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(contentStream);
            }

            string valStr;

            Assert.True(responseMessage.IsSuccessStatusCode);
            Assert.NotNull(responseMessage.Content.Headers.ContentType);
            Assert.Equal(contentTypeStr, responseMessage.Content.Headers.ContentType.MediaType);

            Assert.True(currencies.TryGetValue(currencyCode, out valStr));
            Assert.Equal(currencyName, valStr);
        }

        [Theory]
        [InlineData("AAA")]
        [InlineData("xz")]
        [InlineData("1234")]
        public async Task NotExistingCurrenciesNotFoundTest(string code)
        {
            string url = $"currencies.json?app_id={apiKey}";

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
            Dictionary<string, string> currencies;

            using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                currencies = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(contentStream);
            }


            Assert.True(responseMessage.IsSuccessStatusCode);
            Assert.NotNull(responseMessage.Content.Headers.ContentType);
            Assert.Equal(contentTypeStr, responseMessage.Content.Headers.ContentType.MediaType);

            Assert.False(currencies.ContainsKey(code));
        }
    }
}