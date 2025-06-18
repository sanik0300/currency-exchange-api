using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchangeAPI.Controllers
{
    [ApiController]
    [Route("/")]
    public class MainController : ControllerBase
    {
        private readonly IDbService dbService;
        private readonly CurrencyInfoServiceOXR currencyInfoService;
        private readonly RatesServiceEXRAPI ratesService;

        public MainController(IDbService dbService, IServiceProvider isp)
        {
            this.dbService = dbService;

            currencyInfoService = (CurrencyInfoServiceOXR)isp.GetService(typeof(CurrencyInfoServiceOXR));
            ratesService = (RatesServiceEXRAPI)isp.GetService(typeof(RatesServiceEXRAPI));
        }

        [HttpGet]
        [Route("list")]
        public async Task<JsonResult> GetCurrenciesList()
        {
            List<Currency> allInDb = await dbService.GetAllCurrencies();
            return new JsonResult(allInDb);
        }

        [HttpGet]
        [Route("rates")]
        public async Task<JsonResult> GetExchangeRates(string code)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddCurrency(string code)
        {
            Currency? alreadyExisting = await dbService.GetCurrency(code);

            if(alreadyExisting!=null)
            {
                return BadRequest($"Currency with the code {code} ({alreadyExisting.Name}) already exists");
            }

            Currency result;
            try {
                result = await currencyInfoService.GetNewCurrency(code);
            }
            catch(KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }

            dbService.AddCurrency(result);

            return NoContent();
        }
    }
}