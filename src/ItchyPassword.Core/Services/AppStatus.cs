namespace ItchyPassword.Core.Services;

/// <summary>
/// Represents the high-level state of the application.
/// </summary>
public enum AppStatus
{
    /// <summary>
    /// User has not provided a master key. (Start state)
    /// </summary>
    NotLoaded,

    /// <summary>
    /// Master key provided, currently attempting to authenticate with the vault connector.
    /// Menu should remain hidden during this phase to avoid flickering.
    /// </summary>
    Loading,

    /// <summary>
    /// Vault access granted, currently loading or migrating vault data.
    /// Menu can be visible during this potentially long-running phase.
    /// </summary>
    LoadingVault,

    /// <summary>
    /// Vault is successfully loaded and decrypted.
    /// </summary>
    Loaded,

    /// <summary>
    /// Master key provided but no reader connector is configured/valid.
    /// Vault cannot be loaded.
    /// </summary>
    SetupRequired,

    /// <summary>
    /// An error occurred during the load process (e.g. decryption failed, network error).
    /// </summary>
    Error
}
