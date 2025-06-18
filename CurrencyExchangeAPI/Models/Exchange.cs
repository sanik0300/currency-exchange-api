using System.ComponentModel.DataAnnotations;

namespace CurrencyExchangeAPI.Models
{
    public class Exchange
    {
        [Length(3, 3)]
        public string BaseCode { get; set; } = default!;
        
        [Length(3, 3)]
        public string PriceCode { get; set; } = default!;

        [Range(double.Epsilon, double.MaxValue)]
        public double Rate { get; set; }

        public DateTime MeasuredAt { get; set; }
    }
}
