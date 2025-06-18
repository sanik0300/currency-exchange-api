using CurrencyExchangeAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyExchangeAPI.Controllers
{
    [ApiController]
    [Route("/")]
    public class MainController : ControllerBase
    {
        [HttpGet]
        [Route("list")]
        public async Task<JsonResult> GetCurrenciesList()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("rates")]
        public async Task<JsonResult> GetExchangeRates(string code)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("add")]
        public async Task<JsonResult> AddCurrency(Currency entity)
        {
            throw new NotImplementedException();
        }
    }
}