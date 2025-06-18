using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;
using System.Data;
using static Dapper.SqlMapper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyExchangeApiTest.Unit
{
    public sealed class AppDbServiceTest : IDisposable
    {
        private const string connectionStringPath = "test-db-connection-string.txt",

                             insertCurrencyQuery = "INSERT INTO currencies (code, name) VALUES(@Code, @Name)",
                             selectCurrenciesQuery = "SELECT * FROM currencies",

                             insertExchangeQuery = "INSERT INTO exchanges (base_code, price_code, rate, measured_at) VALUES (@BaseCode, @PriceCode, @Rate, @MeasuredAt)",

                             cleanDoubleQuery = "DELETE FROM currencies; DELETE FROM exchanges;";

        private readonly ApplicationDbServicePostgres dbService;
        private readonly IDbConnection dbConnection;

        public AppDbServiceTest()
        {
            string testConnStr = File.ReadAllText(connectionStringPath);

            var mock = new Mock<IConfiguration>();
            mock.Setup(a => a["Data:Postgres:Main"]).Returns(testConnStr);

            dbService = new ApplicationDbServicePostgres(mock.Object);

            dbConnection = new NpgsqlConnection(testConnStr);
            dbConnection.Open();
        }

        [Fact]
        public async Task GetExistingCurrencyTest()
        {
            Currency[] testCurrencies = new Currency[2]
            {
                new Currency() { Code = "UAH", Name="Ukrainian Hryvnia" },
                new Currency() { Code = "CAD", Name="Canadian Dollar" }
            };

            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);

            Currency? result = await dbService.GetCurrency("CAD");

            Assert.NotNull(result);
            Assert.Equal("CAD", result.Code);   
        }

        [Fact]
        public async Task GetNonExistentCurrencyEmptyTest()
        {
            Currency[] testCurrencies = new Currency[2]
            {
                new Currency() { Code = "UAH", Name="Ukrainian Hryvnia" },
                new Currency() { Code = "CAD", Name="Canadian Dollar" }
            };

            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);

            Currency? result = await dbService.GetCurrency("INR");

            Assert.Null(result);
        }

        [Fact]
        public async Task AddingCurrencyOkTest()
        {
            Currency entity = new Currency() { Code="ARS", Name= "Argentine Peso" };
           
            Exception exc = await Record.ExceptionAsync(async () => await dbService.AddCurrency(entity));           

            IEnumerable<Currency> tableContent = await dbConnection.QueryAsync<Currency>(selectCurrenciesQuery);
            Currency result = tableContent.FirstOrDefault();

            Assert.Null(exc);
            Assert.Equal(1, tableContent.Count());
            Assert.Equal(entity, result);
        }

        [Fact]
        public async Task AddingRepetitiveCurrencyErrorTest()
        {
            Currency sameEntity = new Currency() { Code = "ARS", Name = "Argentine Peso" };
            await dbService.AddCurrency(sameEntity);

            Exception exc = await Record.ExceptionAsync(async () => await dbService.AddCurrency(sameEntity));

            int rowsCount = await dbConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM currencies");

            Assert.NotNull(exc);
            Assert.Equal(1, rowsCount);
        }

        [Fact]
        public async Task GetAllCurrenciesTest()
        {
            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "UAH", Name="Ukrainian Hryvnia" },
                new Currency() { Code = "CAD", Name="Canadian Dollar" },
                new Currency() { Code = "HKD", Name="Hong Kong Dollar" }
            };

            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);

            List<Currency> allCurrencies = await dbService.GetAllCurrencies();

            Assert.Equal(testCurrencies.Length, allCurrencies.Count);
            for(int i = 0; i < testCurrencies.Length; i++)
            {
                Assert.Equal(testCurrencies[i], allCurrencies[i]);
            }
        }

        [Fact]
        public async Task AddMatchingExchangesOkTest()
        {
            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "USD", Name="" },
                new Currency() { Code = "GBP", Name="" },
                new Currency() { Code = "BGN", Name="" }
            };
            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);

            Exchange[] testExchanges = new Exchange[3]
            {
                new Exchange() { BaseCode="USD", PriceCode="GBP", Rate=0.7365, MeasuredAt =DateTime.Now },
                new Exchange() { BaseCode="GBP", PriceCode="BGN", Rate = 2.2905, MeasuredAt =default },
                new Exchange() { BaseCode="BGN", PriceCode="USD", Rate =0.5889, MeasuredAt =default }
            };

            Exception exc = await Record.ExceptionAsync(async() => await dbService.AddExchangeRates(testExchanges));

            Exchange[] resultExchanges = (await dbConnection.QueryAsync<Exchange>("SELECT * FROM exchanges")).ToArray();

            Assert.Null(exc);
            Assert.Equal(testExchanges.Length, resultExchanges.Length);
            for(int i = 0; i < testExchanges.Length; i++)
            {
                Assert.Equal(testExchanges[i], resultExchanges[i]);
            }
        }

        [Theory]
        [InlineData("USD", "AMD")]
        [InlineData("KRW", "USD")]
        [InlineData("JPY", "EUR")]
        public async Task AddUnmatchingExchangesErrorTest(string baseCode, string priceCode)
        {
            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "USD", Name="" },
                new Currency() { Code = "GBP", Name="" },
                new Currency() { Code = "BGN", Name="" }
            };
            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);

            Exchange testExchange = new Exchange() { BaseCode = baseCode, PriceCode = priceCode, Rate = default, MeasuredAt = default };

            Exception exc = await Record.ExceptionAsync(async () => await dbService.AddExchangeRates([testExchange]));

            int rowsCount = await dbConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM exchanges");

            Assert.NotNull(exc);
            Assert.Equal(0, rowsCount);
        }

        [Fact]
        public async Task GetRatesForExistingCurrencyTest()
        {
            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "USD", Name="" },
                new Currency() { Code = "GBP", Name="" },
                new Currency() { Code = "BGN", Name="" }
            };

            Exchange[] testExchanges = new Exchange[6]
            {
                new Exchange() { BaseCode="USD", PriceCode="GBP", Rate=0.7365, MeasuredAt =DateTime.Now },
                new Exchange() { BaseCode="USD", PriceCode="BGN", Rate = 1.6912, MeasuredAt =default },

                new Exchange() { BaseCode="GBP", PriceCode="USD", Rate=1.3471, MeasuredAt =DateTime.Now },
                new Exchange() { BaseCode="GBP", PriceCode="BGN", Rate = 2.2905, MeasuredAt =default },

                new Exchange() { BaseCode="BGN", PriceCode="GBP", Rate=0.4366, MeasuredAt =DateTime.Now },
                new Exchange() { BaseCode="BGN", PriceCode="USD", Rate =0.5889, MeasuredAt =default }
            };

            await dbConnection.ExecuteAsync(insertCurrencyQuery, testCurrencies);
            await dbConnection.ExecuteAsync(insertExchangeQuery, testExchanges);

            List<Exchange> resultList = await dbService.GetExchangeRatesFor("GBP");

            Assert.NotNull(resultList);
            Assert.Equal(2, resultList.Count);
            foreach (Exchange e in resultList)
            {
                Assert.Equal("GBP", e.BaseCode);
            }
        }

        [Fact]
        public async Task GetRatesNonExistentCurrencyTest()
        {
            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "GBP", Name=default },
                new Currency() { Code = "USD", Name=default },
                new Currency() { Code = "BGN", Name=default }
            };

            Exchange[] testExchanges = new Exchange[6]
            {
                new Exchange() { BaseCode="USD", PriceCode="GBP", Rate=0.7365, MeasuredAt =default },
                new Exchange() { BaseCode="USD", PriceCode="BGN", Rate = 1.6912, MeasuredAt =default },

                new Exchange() { BaseCode="GBP", PriceCode="USD", Rate=1.3471, MeasuredAt =default },
                new Exchange() { BaseCode="GBP", PriceCode="BGN", Rate = 2.2905, MeasuredAt =default },

                new Exchange() { BaseCode="BGN", PriceCode="GBP", Rate=0.4366, MeasuredAt =default },
                new Exchange() { BaseCode="BGN", PriceCode="USD", Rate =0.5889, MeasuredAt =default }
            };

            List<Exchange> resultList = await dbService.GetExchangeRatesFor("EUR");

            Assert.NotNull(resultList);
            Assert.Empty(resultList);
        }

        public void Dispose()
        {
            using(IDbCommand cleanCommand = dbConnection.CreateCommand())
            {
                cleanCommand.CommandText = cleanDoubleQuery;
                cleanCommand.ExecuteNonQuery();
            }
            dbConnection.Close();  
            dbConnection.Dispose();
            dbService.Dispose();
        }
    }
}
