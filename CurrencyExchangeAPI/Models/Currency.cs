using CurrencyExchangeAPI.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CurrencyExchangeAPI.Models
{
    public class Currency
    {
        [Length(3, 3)]
        public string Code { get; set; } = default!;

        public string? Name { get; set; } = default!;
    }
}
