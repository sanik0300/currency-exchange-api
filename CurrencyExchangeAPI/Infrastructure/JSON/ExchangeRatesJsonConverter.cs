using CurrencyExchangeAPI.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyExchangeAPI.Infrastructure
{
    public class ExchangeRatesJsonConverter : JsonConverter<List<Exchange>>
    {
        public override List<Exchange>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string code = default;
            double rate = default;

            List<Exchange> result = new List<Exchange>();

            do {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        code = reader.GetString();
                        break;
                    case JsonTokenType.Number:
                        rate = reader.GetDouble();

                        result.Add(new Exchange() { PriceCode = code, Rate = rate });
                        break;
                    default:
                        continue;
                }
            }
            while (reader.Read());

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<Exchange> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
