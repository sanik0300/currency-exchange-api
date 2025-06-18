using CurrencyExchangeAPI.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyExchangeAPI.Infrastructure
{
    public class CurrencyListJsonConverter : JsonConverter<List<Currency>>
    {
        public override List<Currency>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string code = default, 
                   name = default;
            
            List<Currency> result = new List<Currency>();

            do {
                switch(reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        code = reader.GetString();
                        break;
                    case JsonTokenType.String:
                        name = reader.GetString();

                        result.Add(new Currency() { Code = code, Name = name });
                        break;
                    default:
                        continue;
                }
            }
            while(reader.Read());

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<Currency> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
