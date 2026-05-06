namespace ItchyPassword.Core.Models;

/// <summary>
/// Result of a passkey enrollment attempt.
/// </summary>
public readonly struct PasskeyEnrollResult
{
    /// <summary>
    /// True if enrollment succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable error message when <see cref="Success"/> is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static PasskeyEnrollResult Successful()
    {
        return new PasskeyEnrollResult { Success = true, ErrorMessage = null };
    }

    public static PasskeyEnrollResult Failed(string errorMessage)
    {
        return new PasskeyEnrollResult { Success = false, ErrorMessage = errorMessage };
    }
}
