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
    public class MainController : ControllerBase
    {
        private readonly IDbService dbService;
        private readonly CurrencyInfoServiceOXR currencyInfoService;
        private readonly RatesServiceEXRAPI ratesService;
        
        private readonly IMemoryCache memoryCache;
        private readonly string cacheKeyForRates;

        public MainController(IDbService dbService, IServiceProvider isp, IMemoryCache memoryCache, IConfiguration conf)
        {
            this.dbService = dbService;
            this.memoryCache = memoryCache;

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
                return NoContent();
            }
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
                return NotFound();
            }

            if(allInDb.Count == 1)
            {
                return BadRequest($"The requested currency is the only one in the available list. Add more to request exchange rates between them.");
            }

            List<Exchange>? allFromCache = memoryCache.Get<List<Exchange>>(cacheKeyForRates);

            if (allFromCache!=null)
            {
                List<Exchange> _basedOnRequested = allFromCache.Where(e => e.BaseCode == code).ToList();

                if(_basedOnRequested.Count == allInDb.Count-1)
                {
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
                return NotFound(e.Message);
            }

            List<Currency> otherCurrencies = await dbService.GetAllCurrencies();

            await dbService.AddCurrency(result);

            if (otherCurrencies.Any())
            {
                List<Exchange> exchangeRatesBasedOnNew = await ratesService.GetRatesFor(result.Code, otherCurrencies);
                await dbService.AddExchangeRates(exchangeRatesBasedOnNew);
            }

            return Ok($"Now there are {otherCurrencies.Count+1} currencies available");
        }
    }
}