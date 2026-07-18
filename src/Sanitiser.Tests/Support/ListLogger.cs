using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Sanitiser.Tests.Support;

/// <summary>
/// A minimal <see cref="ILogger{T}"/> that captures log entries so tests can assert on them.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));

    public bool HasWarningContaining(string text) => Entries.Any(entry =>
        entry.Level == LogLevel.Warning && entry.Message.Contains(text, StringComparison.OrdinalIgnoreCase));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
