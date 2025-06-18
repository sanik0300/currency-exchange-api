using CurrencyExchangeAPI;
using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyExchangeApiTest.Integration
{
    public class RatesInfoServiceTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly RatesServiceEXRAPI _service;

        public RatesInfoServiceTest(WebApplicationFactory<Program> factory)
        {
            _service = (RatesServiceEXRAPI)factory.Services.GetService(typeof(RatesServiceEXRAPI));
        }

        [Theory]
        [InlineData("EUR")]
        [InlineData("BGN")]
        [InlineData("AUD")]
        public async Task FindingExistingRatesTest(string baseCode)
        {
            Currency[] requestedCurrencies = new Currency[4]
            {
                new Currency() { Code = "UAH" }, 
                new Currency() { Code = "RON" },
                new Currency() { Code = "ILS" },
                new Currency() { Code = "KZT" }
            };

            List<Exchange> foundExchanges = null;

            Exception exc = await Record.ExceptionAsync(async () => foundExchanges = await _service.GetRatesFor(baseCode, requestedCurrencies));

            Assert.Null(exc);
            Assert.NotNull(foundExchanges);
            Assert.NotEmpty(foundExchanges);

            Assert.Equal(requestedCurrencies.Length, foundExchanges.Count());

            for(int i = 0; i < requestedCurrencies.Length; i++)
            {
                Assert.True(requestedCurrencies.Any(c => c.Code == foundExchanges[i].PriceCode));
                Assert.Equal(baseCode, foundExchanges[i].BaseCode);
            }
        }

        [Fact]
        public async Task NonExistingRatesEmptyTest()
        {
            string baseCode = "AAA";

            Currency[] requestedCurrencies = new Currency[4]
            {
                new Currency() { Code = "UAH" },
                new Currency() { Code = "RON" },
                new Currency() { Code = "ILS" },
                new Currency() { Code = "KZT" }
            };

            List<Exchange> foundExchanges = null;

            Exception exc = await Record.ExceptionAsync(async () => foundExchanges = await _service.GetRatesFor(baseCode, requestedCurrencies));

            Assert.Null(foundExchanges);
            Assert.NotNull(exc);
            Assert.IsType(typeof(KeyNotFoundException), exc);
            Assert.Equal("Currency code AAA is unknown!", exc.Message);
        }
    }
}
