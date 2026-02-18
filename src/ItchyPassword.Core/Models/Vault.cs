namespace ItchyPassword.Core.Models;

public enum VaultItemType
{
    StaticKey = 0,
    Secret = 1
}

/// <summary>
/// Parameters for Secret-type vault items (encrypted secrets).
/// </summary>
public class SecretParameters
{
    public int CipherVersion { get; set; } = 3;
    public string Encoding { get; set; } = "base58";
}

/// <summary>
/// Parameters for StaticKey-type vault items (deterministic password generation).
/// </summary>
public class StaticKeyParameters
{
    public string PublicPart { get; set; } = string.Empty;
    public string Alphabet { get; set; } = string.Empty;
    public int Length { get; set; } = 64;
    public int Version { get; set; } = 2;
    public string Encoding { get; set; } = "base58";
}

public class VaultItem
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required VaultItemType Type { get; set; }

    // For "Secret": The encrypted string.
    // For "StaticKey": unused (public part is in StaticKeyParameters).
    public string Content { get; set; } = "";

    /// <summary>
    /// Parameters for Secret-type items. Must be null when Type is StaticKey.
    /// </summary>
    public SecretParameters? SecretParameters { get; set; }

    /// <summary>
    /// Parameters for StaticKey-type items. Must be null when Type is Secret.
    /// </summary>
    public StaticKeyParameters? StaticKeyParameters { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public required DateTimeOffset LastModified { get; set; }
}

public class Vault
{
    public required int Version { get; init; }
    public required List<VaultItem> Items { get; init; }
}
