
using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;

namespace CurrencyExchangeAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                            .AddJsonOptions(options =>
                                { 
                                    options.JsonSerializerOptions.Converters.Insert(0, new CurrencyListJsonConverter());
                                    options.JsonSerializerOptions.Converters.Insert(1, new ExchangeRatesJsonConverter());
                                });
            builder.Services.AddHttpClient<CurrencyInfoServiceOXR>();
            builder.Services.AddHttpClient<RatesServiceEXRAPI>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

           

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
