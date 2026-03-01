namespace ItchyPassword.Core.Services;

/// <summary>
/// Manages the application's global state and navigation flow.
/// </summary>
public interface IAppState
{
    /// <summary>
    /// Gets the current status of the application.
    /// </summary>
    AppStatus Status { get; }

    /// <summary>
    /// Gets a user-friendly message describing the current operation or error.
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Gets or sets the search query used to filter vault items.
    /// </summary>
    string SearchQuery { get; set; }

    /// <summary>
    /// Event raised when the application state changes.
    /// </summary>
    event Action OnChange;

    /// <summary>
    /// Attempts to unlock the vault with the provided key.
    /// </summary>
    /// <param name="key">The master key.</param>
    Task UnlockAsync(byte[] key);

    /// <summary>
    /// Locks the vault, clears the master key, and resets the application state.
    /// </summary>
    void Lock();

    /// <summary>
    /// Retries the unlock process using the existing master key.
    /// </summary>
    Task RetryUnlockAsync();

    /// <summary>
    /// Navigates to the configuration page if possible.
    /// </summary>
    void Configure();
}
