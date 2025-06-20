using CurrencyExchangeAPI;
using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using System.Data;
using Xunit.Sdk;
using static Dapper.SqlMapper;

namespace CurrencyExchangeApiTest.Unit
{
    public class RefreshBackgroundServiceTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private const string connectionStringPath = "test-db-connection-string.txt",
                             cleanDoubleQuery = "DELETE FROM currencies; DELETE FROM exchanges;";

        private readonly WebApplicationFactory<Program> factory;

        private readonly Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
        private readonly IDbConnection dbConnection;

        public RefreshBackgroundServiceTest(WebApplicationFactory<Program> factory)
        {
            string testConnStr = File.ReadAllText(connectionStringPath);

            var customFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    Dictionary<string, string> inMemory = new Dictionary<string, string>
                    {
                        ["Data:Postgres:Main"] = testConnStr
                    };
                    configBuilder.AddInMemoryCollection(inMemory);
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddScoped<IDbService, ApplicationDbServicePostgres>();
                });
            });

            this.factory = customFactory;

            dbConnection = new NpgsqlConnection(testConnStr);
            dbConnection.Open();
        }

        [Fact]  

        public async Task NotEnoughCurrenciesNoRefreshesTest()
        {
            mockConfig.Setup(a => a["RefreshMinutes"]).Returns("1");

            IServiceScopeFactory scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();    
            RatesRefreshService refreshService = new RatesRefreshService(mockConfig.Object, scopeFactory);

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            Currency entity = new Currency() { Code = "UAH", Name = "" };
            await dbConnection.ExecuteAsync("INSERT INTO currencies (code, name) VALUES (@Code, @Name)", entity);

            await refreshService.StartAsync(token);

            using (PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(2.5)))
            {
                await timer.WaitForNextTickAsync(token);
                await cancelTokenSource.CancelAsync();
            }

            IEnumerable<Exchange> exchanges = await dbConnection.QueryAsync<Exchange>("SELECT * FROM EXCHANGES");

            cancelTokenSource.Dispose();

            Assert.Empty(exchanges);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("3")]
        public async Task EnoughCurrenciesRefreshesTest(string minutesInterval)
        {
            mockConfig.Setup(a => a["RefreshMinutes"]).Returns(minutesInterval);

            IServiceScopeFactory scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
            RatesRefreshService refreshService = new RatesRefreshService(mockConfig.Object, scopeFactory);

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            Currency[] testCurrencies = new Currency[3]
            {
                new Currency() { Code = "UAH", Name="Ukrainian Hryvnia" },
                new Currency() { Code = "CAD", Name="Canadian Dollar" },
                new Currency() { Code = "HKD", Name="Hong Kong Dollar" }
            };
            await dbConnection.ExecuteAsync("INSERT INTO currencies (code, name) VALUES (@Code, @Name)", testCurrencies);

            await refreshService.StartAsync(token);

            using (PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(Convert.ToInt32(minutesInterval)*2.5)))
            {
                await timer.WaitForNextTickAsync(token);
                await cancelTokenSource.CancelAsync();
            }

            IEnumerable<Exchange> exchanges = await dbConnection.QueryAsync<Exchange>("SELECT * FROM EXCHANGES");

            int tableSize = testCurrencies.Length*(testCurrencies.Length-1);

            Assert.NotEmpty(exchanges);
            Assert.Equal(tableSize, exchanges.Count());

            cancelTokenSource.Dispose();
        }

        public void Dispose()
        {
            using (IDbCommand cleanCommand = dbConnection.CreateCommand())
            {
                cleanCommand.CommandText = cleanDoubleQuery;
                cleanCommand.ExecuteNonQuery();
            }

            dbConnection.Close();
            dbConnection.Dispose();
        }
    }
}
