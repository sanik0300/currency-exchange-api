using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Net;

namespace CurrencyExchangeAPI.Controllers
{
    [ApiController]
    [Route("/")]
    public partial class MainController : ControllerBase
    {
        private readonly IDbService dbService;
        private readonly CurrencyInfoServiceOXR currencyInfoService;
        private readonly RatesServiceEXRAPI ratesService;
        
        private readonly IMemoryCache memoryCache;
        private readonly string cacheKeyForRates;

        private readonly ILogger<MainController> logger;

        public MainController(IDbService dbService, IServiceProvider isp, IMemoryCache memoryCache, 
                              IConfiguration conf, ILogger<MainController> logger)
        {
            this.dbService = dbService;
            this.memoryCache = memoryCache;
            this.logger = logger;

            currencyInfoService = (CurrencyInfoServiceOXR)isp.GetService(typeof(CurrencyInfoServiceOXR));
            ratesService = (RatesServiceEXRAPI)isp.GetService(typeof(RatesServiceEXRAPI));

            cacheKeyForRates = conf["CacheKeys:ExchangeRates"];
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetCurrenciesList()
        {
            List<Currency> allInDb = await dbService.GetAllCurrencies();

            if(!allInDb.Any())
            {
                LogNoCurrenciesRetrieved();
                return NoContent();
            }

            LogCurrenciesRetrieval(allInDb.Count);
            return new JsonResult(allInDb);
        }

        [HttpGet]
        [Route("rates")]
        [UppercaseCodeFilter]
        public async Task<IActionResult> GetExchangeRates(string code)
        {
            List<Currency> allInDb = await dbService.GetAllCurrencies();

            Currency? requested = allInDb.FirstOrDefault(x => x.Code == code);
            if (requested == null)
            {
                LogSavedCurrencyNotFound(code);
                return NotFound();
            }

            if(allInDb.Count == 1)
            {
                LogOnlyOneCurrency(code);
                return BadRequest($"The requested currency is the only one in the available list. Add more to request exchange rates between them.");
            }

            List<Exchange>? allFromCache = memoryCache.Get<List<Exchange>>(cacheKeyForRates);

            if (allFromCache!=null)
            {
                List<Exchange> _basedOnRequested = allFromCache.Where(e => e.BaseCode == code).ToList();

                if(_basedOnRequested.Count == allInDb.Count-1)
                {
                    LogRatesRetrieved(code, "cache");
                    return new JsonResult(_basedOnRequested);
                }
            }

            List<Exchange> fromExtAPI = await ratesService.GetRatesFor(code, allInDb);

            IEnumerable<Exchange> fromExtApiWithoutSelf = fromExtAPI.Where(e => Math.Abs(e.Rate - 1) > 0.00001);

            if(allFromCache == null)
            {
                memoryCache.Set(cacheKeyForRates, new List<Exchange>(fromExtApiWithoutSelf));
            }
            else {
                List<Exchange> partialCacheUpd = allFromCache.Where(e => e.BaseCode != code).Concat(fromExtApiWithoutSelf).ToList();
                memoryCache.Set(cacheKeyForRates, partialCacheUpd);
            }

            LogRatesRetrieved(code, "external API");
            return new JsonResult(fromExtApiWithoutSelf.ToList());
        }

        [HttpPost]
        [Route("add")]
        [UppercaseCodeFilter]
        public async Task<IActionResult> AddCurrency(string code)
        {
            Currency? alreadyExisting = await dbService.GetCurrency(code);

            if(alreadyExisting!=null)
            {
                LogAlreadySaved(code);
                return new ObjectResult($"Currency with the code {code} ({alreadyExisting.Name}) already present in the list.") { 
                    StatusCode = (int)HttpStatusCode.MethodNotAllowed
                };
            }

            Currency result;
            try {
                result = await currencyInfoService.GetNewCurrency(code);
            }
            catch(KeyNotFoundException e)
            {
                LogDoesNotExist(code);
                return NotFound(e.Message);
            }

            List<Currency> otherCurrencies = await dbService.GetAllCurrencies();

            LogCurrencySaved(code, otherCurrencies.Count + 1);
            await dbService.AddCurrency(result);

            if (otherCurrencies.Any())
            {
                LogRatesSaved(code, otherCurrencies.Count);
                List<Exchange> exchangeRatesBasedOnNew = await ratesService.GetRatesFor(result.Code, otherCurrencies);
                await dbService.AddExchangeRates(exchangeRatesBasedOnNew);
            }

            return NoContent();
        }

        [LoggerMessage(100, LogLevel.Information, "Retrieved a list of saved currencies, with {Count} entries")]
        partial void LogCurrenciesRetrieval(int Count);

        [LoggerMessage(101, LogLevel.Warning, "Asked for a list of saved currencies, but there are no entries")]
        partial void LogNoCurrenciesRetrieved();


        [LoggerMessage(200, LogLevel.Information, "Retrieved exchange rates for currency {BaseCode} from {Source}")]
        partial void LogRatesRetrieved(string BaseCode, string Source);

        [LoggerMessage(201, LogLevel.Warning, "Currency with a code {Code} not found among saved entries, cannot retrieve rates")]
        partial void LogSavedCurrencyNotFound(string Code);

        [LoggerMessage(202, LogLevel.Warning, "Currency with a code {Code} is the only one saved, cannot retrieve rates")]
        partial void LogOnlyOneCurrency(string Code);


        [LoggerMessage(300, LogLevel.Information, "Currency {Code} info saved, now there are {NewCount} entries")]
        partial void LogCurrencySaved(string Code, int NewCount);

        [LoggerMessage(301, LogLevel.Warning, "Asked to save currency {Code}, but it already exists in app data")]
        partial void LogAlreadySaved(string Code);

        [LoggerMessage(302, LogLevel.Error, "Not found info about currency with a code {Code} at the external API")]
        partial void LogDoesNotExist(string Code);


        [LoggerMessage(310, LogLevel.Information, "Saved exchange rates of {BaseCode} to {Count} other saved currencies")]
        partial void LogRatesSaved(string BaseCode, int Count);
    }
}