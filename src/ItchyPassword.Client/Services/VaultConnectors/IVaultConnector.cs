namespace ItchyPassword.Client.Services.VaultConnectors;

public record struct ConfigStorageKey(string Config, string Storage);

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
    /// Attempts to connect to the vault storage provider using the current configuration.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the connection was successful; otherwise, false.</returns>
    Task<bool> ConnectAsync();

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
    /// Gets the configuration dictionary for the connector.
    /// </summary>
    Dictionary<string, string> Configuration { get; }
}
