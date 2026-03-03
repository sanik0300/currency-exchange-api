using System.Collections.Concurrent;

namespace CurrencyExchangeAPI.Infrastructure.Logging
{
    public class TextFileLoggerProvider(string loggingFilePath) : ILoggerProvider
    {
        private bool _disposed;
        private readonly ConcurrentDictionary<string, TextFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

        public ILogger CreateLogger(string categoryName)
        {
            TextFileLogger? loggerToReturn = null;
            if (_loggers.TryGetValue(categoryName, out loggerToReturn))
            {
                return loggerToReturn;
            }

            loggerToReturn = new TextFileLogger(loggingFilePath);
            _loggers[categoryName] = loggerToReturn;
            return loggerToReturn;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {
                _loggers.Clear();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
