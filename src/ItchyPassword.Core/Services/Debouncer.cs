namespace ItchyPassword.Core.Services;

/// <summary>
/// Debounces async actions: each call cancels the previous pending delay.
/// Only the last invocation within the delay window executes.
/// </summary>
public sealed class Debouncer(int delayMs = 300) : IDisposable
{
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>
    /// Waits for the configured delay. Returns <c>false</c> if the delay completed
    /// without being cancelled by a newer call, <c>true</c> otherwise.
    /// </summary>
    public async Task<bool> IsBouncedAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(delayMs, _cts.Token);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    /// <summary>
    /// Cancels any pending delay immediately.
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
