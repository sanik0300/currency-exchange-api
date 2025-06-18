using CurrencyExchangeAPI.Models;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrencyExchangeAPI.Infrastructure;

namespace CurrencyExchangeApiTest.Unit
{
    public sealed class JsonCurrencyTest
    {
        private readonly JsonSerializerOptions optionsLikeInProgram;

        public JsonCurrencyTest()
        {
            optionsLikeInProgram = new JsonSerializerOptions();
            optionsLikeInProgram.Converters.Insert(0, new CurrencyListJsonConverter());
        }

        [Theory]
        [InlineData(new object[] { new string[] { "ALL", "AMD" }, new string[] { "Albanian Lek", "Armenian Dram" } })]
        [InlineData(new object[] { new string[] { "EUR", "THB" }, new string[] { "Euro", "Thai Bat" } })]
        public void MultipleCurrencyInstancesDeserialize(string[] codes, string[] names)
        {
            IEnumerable<string> keyValuePairs = codes.Zip(names, (code, name) => $" \"{code}\" : \"{name}\"");

            StringBuilder jsonBuilder = new StringBuilder("{");
            foreach(string pair in keyValuePairs)
            {
                jsonBuilder.Append(pair);
                jsonBuilder.Append(", ");
            }
            jsonBuilder.Remove(jsonBuilder.Length - 2, 2);
            jsonBuilder.Append(" }");

            List<Currency>? deserializedCurrencies = null;

            Exception ex = Record.Exception(() => deserializedCurrencies = JsonSerializer.Deserialize<List<Currency>>(jsonBuilder.ToString(), optionsLikeInProgram));
        
            Assert.Null(ex);
            Assert.NotNull(deserializedCurrencies);
            Assert.DoesNotContain(null, deserializedCurrencies);

            for(int i = 0; i < codes.Length; i++)
            {
                Assert.Equal(codes[i], deserializedCurrencies[i].Code);
                Assert.Equal(names[i], deserializedCurrencies[i].Name);
            }
        }


        [Theory]
        [InlineData("UAH", "Ukrainian Hryvnia")]
        [InlineData("SEK", "Swedish Krona")]
        [InlineData("RON", "Romanian Leu")]
        public void SingleCurrencyInstanceDeserialize(string currencyCode, string currencyName)
        {
            string json = $"{{ \"{currencyCode}\" : \"{currencyName}\" }}";

            Currency? deserializedCurrency = null;

            Exception ex = Record.Exception(() =>
            {
                deserializedCurrency = JsonSerializer.Deserialize<List<Currency>>(json, optionsLikeInProgram)[0];
            });

            Assert.Null(ex);
            Assert.NotNull(deserializedCurrency);
            Assert.Equal(currencyCode, deserializedCurrency.Code);
            Assert.Equal(currencyName, deserializedCurrency.Name);
        }
    }
}
