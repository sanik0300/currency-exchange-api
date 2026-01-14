using CurrencyExchangeAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace CurrencyExchangeAPI.Infrastructure
{
    public partial class RatesRefreshService : BackgroundService
    {
        private readonly TimeSpan refreshInterval;

        private readonly IServiceScopeFactory serviceFactory;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<RatesRefreshService> logger;

        private readonly string cacheKeyForRates;

        public RatesRefreshService(IConfiguration conf, IServiceScopeFactory serviceFactory, IMemoryCache memoryCache, ILogger<RatesRefreshService> logger)
        {
            this.serviceFactory = serviceFactory;
            this.memoryCache = memoryCache;
            this.logger = logger;

            string minutes = conf["RefreshMinutes"];
            refreshInterval = TimeSpan.FromMinutes(Convert.ToInt32(minutes));

            cacheKeyForRates = conf["CacheKeys:ExchangeRates"];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (PeriodicTimer timer = new PeriodicTimer(refreshInterval))
            {
                while(!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                {
                    using (IServiceScope scope = serviceFactory.CreateScope())
                    {
                        LogIteration();

                        IDbService dbService = scope.ServiceProvider.GetRequiredService<IDbService>();
                        List<Currency> availableCurrencies = await dbService.GetAllCurrencies();

                        if(availableCurrencies.Count < 2) 
                        {
                            LogNoNeedToCache();
                            continue; 
                        }

                        RatesServiceEXRAPI ratesApiService = scope.ServiceProvider.GetRequiredService<RatesServiceEXRAPI>();

                        List<Exchange> exchangesGoingToDb = new List<Exchange>();

                        foreach (Currency currency in availableCurrencies)
                        {
                            List<Exchange> basedOnCrt = await ratesApiService.GetRatesFor(currency.Code, availableCurrencies);
                        
                            exchangesGoingToDb.AddRange(basedOnCrt.Where(e => Math.Abs(e.Rate-1)>0.000001));
                        }

                        var entryOptions = new MemoryCacheEntryOptions()
                                                .SetSlidingExpiration(refreshInterval)
                                                .SetAbsoluteExpiration(refreshInterval*1.1)
                                                .SetPriority(CacheItemPriority.High);

                        await dbService.RefreshExchangeRates(exchangesGoingToDb);
                        memoryCache.Set(cacheKeyForRates, exchangesGoingToDb, entryOptions);
                        
                        LogRatesCaching(exchangesGoingToDb.Count);
                    }
                }
            }
        }

        [LoggerMessage(70, LogLevel.Debug, "Exchange rates caching service iteration")]
        partial void LogIteration();

        [LoggerMessage(80, LogLevel.Information, "Cached {Count} entries of exchange rates")]
        partial void LogRatesCaching(int Count);

        [LoggerMessage(81, LogLevel.Warning, "Less than 2 currency entries are saved, no rates between them to cache")]
        partial void LogNoNeedToCache();
    }
}
