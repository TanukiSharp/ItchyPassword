using System.Collections.ObjectModel;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Captures background errors that would otherwise be invisible to the user.
/// Not for inline errors (e.g., <c>_error</c> fields) or status messages (<c>AppState.StatusMessage</c>).
/// </summary>
public sealed class ErrorLogService
{
    private readonly IReadOnlyList<ErrorLogEntry> _readonlyEntries;
    private readonly List<ErrorLogEntry> _entries = [];
    private readonly Lock _lock = new();

    public ErrorLogService()
    {
        _readonlyEntries = new ReadOnlyCollection<ErrorLogEntry>(_entries);
    }

    /// <summary>
    /// Maximum number of entries retained in memory.
    /// </summary>
    public const int MaxEntries = 100;

    /// <summary>
    /// Event raised when a new error is logged.
    /// </summary>
    public event Action? OnErrorLogged;

    /// <summary>
    /// Gets a snapshot of the current error log entries, most recent first.
    /// </summary>
    public IReadOnlyList<ErrorLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _readonlyEntries;
            }
        }
    }

    /// <summary>
    /// Gets the number of entries currently in the log.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>
    /// Logs an error with optional context about where it occurred.
    /// </summary>
    public void Log(string message, string? source = null, Exception? exception = null)
    {
        ErrorLogEntry entry = new(DateTimeOffset.UtcNow, message, source, exception?.ToString());

        lock (_lock)
        {
            _entries.Insert(0, entry);

            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }
        }

        OnErrorLogged?.Invoke();
    }

    /// <summary>
    /// Clears all entries from the log.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        OnErrorLogged?.Invoke();
    }
}

/// <summary>
/// A single entry in the error log.
/// </summary>
/// <param name="Timestamp">When the error occurred.</param>
/// <param name="Message">A human-readable description of the error.</param>
/// <param name="Source">Optional context about where the error occurred (e.g., component or method name).</param>
/// <param name="Details">Optional exception details (stack trace, etc.).</param>
public sealed record ErrorLogEntry(DateTimeOffset Timestamp, string Message, string? Source, string? Details);
