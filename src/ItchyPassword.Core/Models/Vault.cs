using System.Text.Json.Serialization;

namespace ItchyPassword.Core.Models;

public enum VaultItemTypeV2
{
    StaticKey = 0,
    Secret = 1
}

/// <summary>
/// Data for Secret-type vault items (encrypted secrets).
/// </summary>
public class SecretDataV2
{
    /// <summary>
    /// The encrypted cipher text.
    /// </summary>
    [JsonPropertyName("cipher")]
    public string Cipher { get; init; } = string.Empty;

    [JsonPropertyName("cryptoVersion")]
    public required int CryptoVersion { get; init; }

    [JsonPropertyName("encoding")]
    public required string Encoding { get; init; }
}

/// <summary>
/// Data for StaticKey-type vault items (deterministic password generation).
/// </summary>
public class StaticKeyDataV2
{
    [JsonPropertyName("publicPart")]
    public required string PublicPart { get; init; }

    [JsonPropertyName("alphabet")]
    public required string Alphabet { get; init; }

    [JsonPropertyName("length")]
    public required int Length { get; init; }

    [JsonPropertyName("cryptoVersion")]
    public required int CryptoVersion { get; init; }

    [JsonPropertyName("encodingVersion")]
    public required int EncodingVersion { get; init; }
}

public class VaultItemV2
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required VaultItemTypeV2 Type { get; set; }

    /// <summary>
    /// Data for Secret-type items. Must be null when Type is StaticKey.
    /// Use <see cref="SetData(SecretDataV2)"/> to set.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("secretData")]
    public SecretDataV2? SecretData { get; private set; }

    /// <summary>
    /// Data for StaticKey-type items. Must be null when Type is Secret.
    /// Use <see cref="SetData(StaticKeyDataV2)"/> to set.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("staticKeyData")]
    public StaticKeyDataV2? StaticKeyData { get; private set; }

    /// <summary>
    /// Sets the data for a Secret-type item and clears StaticKeyData.
    /// </summary>
    public void SetData(SecretDataV2 data)
    {
        SecretData = data;
        StaticKeyData = null;
    }

    /// <summary>
    /// Sets the data for a StaticKey-type item and clears SecretData.
    /// </summary>
    public void SetData(StaticKeyDataV2 data)
    {
        StaticKeyData = data;
        SecretData = null;
    }

    [JsonPropertyName("metadata")]
    public List<MetadataEntryV2>? Metadata { get; set; }

    /// <summary>
    /// The date and time the item was created. Immutable after initial creation.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("lastModified")]
    public required DateTimeOffset LastModified { get; set; }
}

/// <summary>
/// A single key-value metadata entry attached to a vault item.
/// </summary>
public class MetadataEntryV2
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class VaultV2
{
    [JsonPropertyName("version")]
    public required int Version { get; init; }

    [JsonPropertyName("items")]
    public required List<VaultItemV2> Items { get; init; }
}
