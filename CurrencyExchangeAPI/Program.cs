using CurrencyExchangeAPI.Infrastructure;
using CurrencyExchangeAPI.Models;
using Microsoft.Extensions.Logging.Console;

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

            bool isInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            string lastSectionName = isInContainer ? "Docker" : "Main";
            builder.Services.Configure<HostingSettings>(
                builder.Configuration.GetSection("Data:Postgres:" + lastSectionName)
            );

            builder.Logging.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
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

            app.Logger.LogInformation("Application has started");
            app.Logger.LogInformation("If Docker detected: {IsDocker}", isInContainer);

            app.Run();

            app.Logger.LogInformation("Application has stopped");
        }
    }
}
