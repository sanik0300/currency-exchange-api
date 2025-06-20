namespace CurrencyExchangeAPI.Models
{
    public interface IDbService
    {
        Task<List<Currency>> GetAllCurrencies();
        Task<Currency?> GetCurrency(string code);

        Task<List<Exchange>> GetExchangeRatesFor(string currencyCode);

        Task AddCurrency(Currency c);

        Task AddExchangeRates(IEnumerable<Exchange> rates);
        Task RefreshExchangeRates(IEnumerable<Exchange> rates);
    }
}
