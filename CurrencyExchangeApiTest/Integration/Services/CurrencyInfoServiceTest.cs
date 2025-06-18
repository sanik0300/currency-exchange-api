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
    public class CurrencyInfoServiceTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly CurrencyInfoServiceOXR _service;

        public CurrencyInfoServiceTest(WebApplicationFactory<Program> factory)
        {
            _service = (CurrencyInfoServiceOXR)factory.Services.GetService(typeof(CurrencyInfoServiceOXR));
        }

        [Theory]
        [InlineData("USD", "United States Dollar")]
        [InlineData("GBP", "British Pound Sterling")]
        [InlineData("TRY", "Turkish Lira")]
        public async Task FindingExistingCurrencyTest(string code, string name)
        {
            Currency foundCurrency=null;

            Exception exc = await Record.ExceptionAsync(async () => foundCurrency = await _service.GetNewCurrency(code));
            
            Assert.Null(exc);
            Assert.NotNull(foundCurrency);
            Assert.Equal(code, foundCurrency.Code);
            Assert.Equal(name, foundCurrency.Name);
        }

        [Fact]
        public async Task ErrorNotExistingCurrencyTest()
        {
            string nonExistentCode = "ABC";
            Currency foundCurrency = null;

            Exception exc = await Record.ExceptionAsync(async () => foundCurrency = await _service.GetNewCurrency(nonExistentCode));

            Assert.Null(foundCurrency);
            Assert.NotNull(exc);
            Assert.IsType(typeof(KeyNotFoundException), exc);
            Assert.Equal("Currency code ABC is unknown!", exc.Message);
        }
    }
}
