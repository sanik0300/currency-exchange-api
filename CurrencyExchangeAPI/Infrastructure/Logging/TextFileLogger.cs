using System.Diagnostics;
using System.Text;

namespace CurrencyExchangeAPI.Infrastructure.Logging
{
    public class TextFileLogger(string filePath) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            StringBuilder logMessageSb = new StringBuilder();
            
            logMessageSb.Append(logLevel.ToString());
            logMessageSb.Append(": [");
            logMessageSb.Append(eventId.Id);
            logMessageSb.Append("] ");

            logMessageSb.Append(formatter(state, exception));

            try
            {
                File.AppendAllLines(filePath, new string[] { logMessageSb.ToString() });
            }
            catch (Exception ex)
            {
                Debug.Write("Could not log: ");
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
