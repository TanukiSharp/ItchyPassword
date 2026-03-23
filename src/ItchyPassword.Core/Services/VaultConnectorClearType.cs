namespace ItchyPassword.Core.Services;

/// <summary>
/// Controls how much state <see cref="IVaultConnector.ClearAsync"/> discards.
/// </summary>
public enum VaultConnectorClearType
{
    /// <summary>
    /// Discards non-sensitive derived state: resolved file IDs, folder IDs, SHA/ETag,
    /// OIDC discovery documents, and DPoP nonces.
    /// In-memory secrets (tokens) and localStorage are untouched.
    /// </summary>
    Cache,

    /// <summary>
    /// Everything included in <see cref="Cache"/>, plus wipes in-memory secrets and removes
    /// all persisted tokens and configuration entries from localStorage.
    /// The connector returns to a fully unconfigured state.
    /// </summary>
    All,
}
