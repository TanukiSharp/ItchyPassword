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
    /// Attempts to load the vault with the provided key.
    /// </summary>
    /// <param name="key">The master key.</param>
    Task LoadAsync(byte[] key, CancellationToken cancellationToken);

    /// <summary>
    /// Unloads the vault, clears the master key, and resets the application state.
    /// </summary>
    Task UnloadAsync();

    /// <summary>
    /// Retries the load process using the existing master key.
    /// </summary>
    Task RetryLoadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reloads the vault from the active read connector without requiring a new master key.
    /// </summary>
    Task ReloadVaultAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Navigates to the configuration page if possible.
    /// </summary>
    void Configure();
}
