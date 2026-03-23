using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Services;

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
    /// should perform the gesture-dependent operation synchronously before the first
    /// <c>await</c>, so that callers preserve the browser gesture simply by calling
    /// this method on the click call-stack.
    /// </para>
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if full read/write access was granted; otherwise <c>false</c>.</returns>
    Task<bool> AccessAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the vault content from the storage provider.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the vault content as a string.</returns>
    Task<string> LoadVaultAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the vault content to the storage provider.
    /// </summary>
    /// <param name="content">The content to save.</param>
    /// <param name="changeHint">A hint describing the change being made.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveVaultAsync(string content, string changeHint, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the configuration for the connector from local storage.
    /// If a master key is available, encrypted secret values are automatically decrypted.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadConfigurationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the current configuration of the connector to local storage.
    /// If a master key is available, secret values are automatically encrypted before persistence.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveConfigurationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value indicating whether a failed access attempt can be retried with a fresh user gesture.
    /// Connectors that rely on browser APIs requiring transient user activation should return <c>true</c>.
    /// </summary>
    bool CanRetryAccess => false;

    /// <summary>
    /// Gets a connector-specific error message set after <see cref="AccessAsync"/> returns <c>false</c>.
    /// When non-null, this message is shown to the user instead of the generic fallback.
    /// </summary>
    string? AccessFailureMessage => null;

    /// <summary>
    /// Gets the configuration entries for the connector.
    /// Each entry describes a user-configurable field with its type, label, and current value.
    /// </summary>
    IReadOnlyList<ConfigurationEntry> Configuration { get; }

    /// <summary>
    /// Clears in-memory secrets: OAuth tokens and any other connector-specific sensitive fields.
    /// Does <b>not</b> touch localStorage or non-sensitive derived state (file IDs, SHA, etc.).
    /// Called by the vault-lock flow so the user's session is preserved for next unlock.
    /// </summary>
    Task ClearSecretsAsync();

    /// <summary>
    /// Clears connector state according to <paramref name="clearType"/>.
    /// <list type="bullet">
    ///   <item><see cref="VaultConnectorClearType.Cache"/>: discards non-sensitive derived state
    ///   (resolved file IDs, folder IDs, SHA/ETag, OIDC discovery documents, DPoP nonces).
    ///   In-memory secrets and localStorage are untouched.</item>
    ///   <item><see cref="VaultConnectorClearType.All"/>: everything in <c>Cache</c>, plus wipes
    ///   in-memory secrets and removes all persisted tokens and configuration entries from
    ///   localStorage. The connector returns to a fully unconfigured state.</item>
    /// </list>
    /// </summary>
    Task ClearAsync(VaultConnectorClearType clearType);
}
