using ItchyPassword.Core.Models;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Defines a connector for a password vault storage provider.
/// </summary>
public interface IVaultConnector
{
    /// <summary>
    /// Gets the unique identifier for this connector.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the display name of the connector.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of the connector.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the connector is configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Attempts to access the vault storage provider using the current configuration.
    /// <para>
    /// Connectors that rely on browser APIs requiring a transient user activation
    /// (e.g. File System Access API) should perform the gesture-dependent operation
    /// synchronously before the first <c>await</c>, so that callers preserve the
    /// browser gesture simply by calling this method on the click call-stack.
    /// </para>
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the access was successful; otherwise, false.</returns>
    Task<bool> AccessAsync();

    /// <summary>
    /// Loads the vault content from the storage provider.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the vault content as a string.</returns>
    Task<string> LoadVaultAsync();

    /// <summary>
    /// Saves the vault content to the storage provider.
    /// </summary>
    /// <param name="content">The content to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveVaultAsync(string content);

    /// <summary>
    /// Loads the configuration for the connector from local storage.
    /// If a master key is available, encrypted secret values are automatically decrypted.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadConfigurationAsync();

    /// <summary>
    /// Saves the current configuration of the connector to local storage.
    /// If a master key is available, secret values are automatically encrypted before persistence.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveConfigurationAsync();

    /// <summary>
    /// Gets a value indicating whether a failed access attempt can be retried with a fresh user gesture.
    /// Connectors that rely on browser APIs requiring transient user activation should return <c>true</c>.
    /// </summary>
    bool CanRetryAccess
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a connector-specific error message set after <see cref="AccessAsync"/> returns <c>false</c>.
    /// When non-null, this message is shown to the user instead of the generic fallback.
    /// </summary>
    string? AccessFailureMessage
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the configuration entries for the connector.
    /// Each entry describes a user-configurable field with its type, label, and current value.
    /// </summary>
    IReadOnlyList<ConfigurationEntry> Configuration { get; }
}
