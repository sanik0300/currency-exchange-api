using CurrencyExchangeAPI.Models;
using Npgsql;
using System.Data;
using Dapper;
using Dapper.FluentMap;
using Microsoft.Extensions.Options;

namespace CurrencyExchangeAPI.Infrastructure
{
    public sealed partial class ApplicationDbServicePostgres : IDbService, IDisposable
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger logger;

        static ApplicationDbServicePostgres()
        {
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new Exchange.DapperModelMap());
            });
        }

        public ApplicationDbServicePostgres(IOptions<HostingSettings> options, ILogger<ApplicationDbServicePostgres> logger)
        {
            this.logger = logger;

            _dbConnection = new NpgsqlConnection(options.Value.ConnectionString);
            _dbConnection.Open();
            
            LogServiceStart();
        }

        public async Task<Currency?> GetCurrency(string code)
        {
            string sql = $"SELECT * FROM currencies WHERE code='{code}'";

            IEnumerable<Currency> currenciesExisting = await _dbConnection.QueryAsync<Currency>(sql);

            return currenciesExisting.Any() ? currenciesExisting.First() : null;
        }

        public async Task AddCurrency(Currency c)
        {
            string sql = "INSERT INTO currencies (code, name) VALUES (@Code, @Name)";
            await _dbConnection.ExecuteAsync(sql, c);
        }

        public async Task RefreshExchangeRates(IEnumerable<Exchange> rates)
        {
            using (IDbTransaction tran = _dbConnection.BeginTransaction()) 
            {
                await _dbConnection.ExecuteAsync("DELETE FROM exchanges;");

                string sql2 = "INSERT INTO exchanges (base_code, price_code, rate, measured_at) VALUES (@BaseCode, @PriceCode, @Rate, @MeasuredAt)";
                await _dbConnection.ExecuteAsync(sql2, rates);
                tran.Commit();
            }
        }
        public async Task AddExchangeRates(IEnumerable<Exchange> rates)
        {
            string sql = "INSERT INTO exchanges (base_code, price_code, rate, measured_at) VALUES (@BaseCode, @PriceCode, @Rate, @MeasuredAt)";
            await _dbConnection.ExecuteAsync(sql, rates);
        }

        public async Task<List<Currency>> GetAllCurrencies()
        {
            return (await _dbConnection.QueryAsync<Currency>("SELECT * FROM currencies")).ToList();
        }

        public async Task<List<Exchange>> GetExchangeRatesFor(string currencyCode)
        {
            return (await _dbConnection.QueryAsync<Exchange>($"SELECT * FROM exchanges WHERE base_code='{currencyCode}'")).ToList();
        }

        public void Dispose()
        {
            _dbConnection.Close();
            _dbConnection.Dispose();

            LogServiceClosure();
        }

        [LoggerMessage(10, LogLevel.Debug, "Service started and DB connection opened")]
        partial void LogServiceStart();

        [LoggerMessage(20, LogLevel.Debug, "DB connection closed and service stopped")]
        partial void LogServiceClosure();
    }
}
