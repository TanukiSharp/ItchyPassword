namespace ItchyPassword.Core.Models;

/// <summary>
/// Describes how two vault contents relate to each other based on their HMAC signatures.
/// </summary>
public enum VaultComparisonStatus
{
    /// <summary>
    /// Both vaults have the same signature (or both are empty).
    /// </summary>
    Identical,

    /// <summary>
    /// The vaults have different signatures.
    /// </summary>
    Different,

    /// <summary>
    /// The remote vault is empty while the local vault has data.
    /// </summary>
    RemoteEmpty,

    /// <summary>
    /// The local vault is empty while the remote vault has data.
    /// </summary>
    LocalEmpty,
}

/// <summary>
/// Result of comparing two vault contents by their HMAC signatures.
/// </summary>
/// <param name="Status">How the two vaults relate.</param>
/// <param name="LocalItemCount">Number of items in the local vault (0 when local is empty).</param>
/// <param name="RemoteItemCount">Number of items in the remote vault (0 when remote is empty).</param>
public record VaultComparisonResult(VaultComparisonStatus Status, int LocalItemCount, int RemoteItemCount);
