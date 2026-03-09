namespace ItchyPassword.Core.Models;

/// <summary>
/// Describes the access capabilities of a vault connector after calling <c>AccessAsync</c>.
/// </summary>
public readonly struct ConnectorAccessResult
{
    /// <summary>
    /// Gets a value indicating whether the connector can read vault data.
    /// </summary>
    public bool CanRead { get; init; }

    /// <summary>
    /// Gets a value indicating whether the connector can write vault data.
    /// </summary>
    public bool CanWrite { get; init; }

    /// <summary>
    /// No access at all (authentication failed or not configured).
    /// </summary>
    public static ConnectorAccessResult None => new() { CanRead = false, CanWrite = false };

    /// <summary>
    /// Read-only access (e.g. a GitHub token without push permission).
    /// </summary>
    public static ConnectorAccessResult ReadOnly => new() { CanRead = true, CanWrite = false };

    /// <summary>
    /// Write-only access (e.g. a connector that cannot read back its own data).
    /// </summary>
    public static ConnectorAccessResult WriteOnly => new() { CanRead = false, CanWrite = true };

    /// <summary>
    /// Full read and write access.
    /// </summary>
    public static ConnectorAccessResult ReadWrite => new() { CanRead = true, CanWrite = true };
}
