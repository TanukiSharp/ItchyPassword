namespace ItchyPassword.Core.Services;

/// <summary>
/// Provides read and write access to the in-memory master key.
/// This service is deliberately separate from VaultSession so that
/// vault connectors can access the master key without creating a circular
/// dependency with the state that holds the connector list.
/// </summary>
public interface IMasterKeyProvider
{
    /// <summary>
    /// Gets or sets the master key as raw bytes.
    /// The caller (typically the UI) converts the user-entered string to UTF-8
    /// bytes once; every downstream consumer works with <c>byte[]</c> directly,
    /// avoiding repeated conversions.
    /// </summary>
    byte[] MasterKey { get; set; }

    /// <summary>
    /// Gets a value indicating whether a non-empty master key has been set.
    /// </summary>
    bool HasMasterKey { get; }
}
