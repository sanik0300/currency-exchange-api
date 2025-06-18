using Dapper.FluentMap.Mapping;
using System.ComponentModel.DataAnnotations;

namespace CurrencyExchangeAPI.Models
{
    public class Exchange
    {
        public class DapperModelMap : EntityMap<Exchange>
        {
            public DapperModelMap()
            {
                Map(e => e.BaseCode).ToColumn("base_code");
                Map(e => e.PriceCode).ToColumn("price_code");
                Map(e => e.MeasuredAt).ToColumn("measured_at");
            }
        }

        [Length(3, 3)]
        public string BaseCode { get; set; } = default!;

        [Length(3, 3)]
        public string PriceCode { get; set; } = default!;

        [Range(double.Epsilon, double.MaxValue)]
        public double Rate { get; set; }

        public DateTime MeasuredAt { get; set; }

        public override bool Equals(object? obj)
        {
            Exchange other = obj as Exchange;

            if (other == null) return false;

            return this.BaseCode == other.BaseCode && this.PriceCode == other.PriceCode
                   && Math.Round(this.Rate, 4) == Math.Round(other.Rate, 4)
                   && this.MeasuredAt.Date == other.MeasuredAt.Date
                   && Math.Truncate(this.MeasuredAt.TimeOfDay.TotalSeconds) == Math.Truncate(other.MeasuredAt.TimeOfDay.TotalSeconds);

        }
    }
}
