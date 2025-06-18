using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyExchangeApiTest.Unit
{
    public sealed class JsonExchangeTest
    {
        private readonly JsonSerializerOptions optionsLikeInProgram;
        public JsonExchangeTest()
        {
            optionsLikeInProgram = new JsonSerializerOptions();
            optionsLikeInProgram.Converters.Insert(0, new ExchangeRatesJsonConverter());
        }

        [Theory]
        [InlineData(new object[] { new string[] { "MXN", "GBP" }, new double[] { 18.9167, 0.7365 } })]
        [InlineData(new object[] { new string[] { "USD", "KRW" }, new double[] { 1, 1359.2628 } })]
        public void MultipleExchangeInstancesDeserialize(string[] codes, double[] rates)
        {
            IEnumerable<string> keyValuePairs = codes.Zip(rates, (code, rate) => $" \"{code}\" : {rate.ToString(CultureInfo.InvariantCulture)}");

            StringBuilder jsonBuilder = new StringBuilder("{");
            foreach (string pair in keyValuePairs)
            {
                jsonBuilder.Append(pair);
                jsonBuilder.Append(", ");
            }
            jsonBuilder.Remove(jsonBuilder.Length - 2, 2);
            jsonBuilder.Append(" }");

            List<Exchange>? deserializedRates = null;

            Exception ex = Record.Exception(() => deserializedRates = JsonSerializer.Deserialize<List<Exchange>>(jsonBuilder.ToString(), optionsLikeInProgram));

            Assert.Null(ex);
            Assert.NotNull(deserializedRates);
            Assert.DoesNotContain(null, deserializedRates);

            for (int i = 0; i < codes.Length; i++)
            {
                Assert.Equal(codes[i], deserializedRates[i].PriceCode);
                Assert.Equal(rates[i], deserializedRates[i].Rate);
            }
        }

        [Theory]
        [InlineData("UAH", 41.5433)]
        [InlineData("CHF", 0.8130)]
        public void SingleExchangeInstanceDeserialize(string currencyCode, double rate)
        {
            string json = $"{{ \"{currencyCode}\" : {rate.ToString(CultureInfo.InvariantCulture)} }}";

            Exchange? deserializedRate = null;

            Exception ex = Record.Exception(() =>
            {
                deserializedRate = JsonSerializer.Deserialize<List<Exchange>>(json, optionsLikeInProgram)[0];
            });

            Assert.Null(ex);
            Assert.NotNull(deserializedRate);
            Assert.Equal(currencyCode, deserializedRate.PriceCode);
            Assert.Equal(rate, deserializedRate.Rate);
        }
    }
}
