
using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Serilog;

namespace CurrencyExchangeAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                            .AddJsonOptions(options =>
                                { 
                                    options.JsonSerializerOptions.Converters.Insert(0, new CurrencyListJsonConverter());
                                    options.JsonSerializerOptions.Converters.Insert(1, new ExchangeRatesJsonConverter());
                                });
            builder.Services.AddHttpClient<CurrencyInfoServiceOXR>();
            builder.Services.AddHttpClient<RatesServiceEXRAPI>();
            builder.Services.AddHostedService<RatesRefreshService>();

            builder.Services.AddScoped<IDbService, ApplicationDbServicePostgres>();
            builder.Services.AddMemoryCache();

            string connStrPostgres = builder.Configuration["Data:Postgres:Main"],
                   tableName = builder.Configuration["Data:Postgres:LogTableName"];

            Log.Logger = new LoggerConfiguration().WriteTo
                                      .PostgreSQL(connStrPostgres, tableName, needAutoCreateTable: true)
                                      .CreateLogger();
            builder.Host.UseSerilog(Log.Logger);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();



            var app = builder.Build();

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
