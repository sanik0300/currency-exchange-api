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
            writer.WriteStartArray();

            foreach (Exchange exc in value)
            {
                writer.WriteStartObject();

                writer.WriteString(nameof(exc.BaseCode), exc.BaseCode);
                writer.WriteString(nameof(exc.PriceCode), exc.PriceCode);
                writer.WriteNumber(nameof(exc.Rate), exc.Rate);
                writer.WriteString(nameof(exc.MeasuredAt), exc.MeasuredAt);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
