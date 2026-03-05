using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using System.Text.Json;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Result of deserializing a vault, including signature integrity status.
/// </summary>
public readonly struct VaultDeserializeResult
{
    public required VaultV2? Vault { get; init; }
    public required VaultSignatureStatus SignatureStatus { get; init; }
}

/// <summary>
/// Indicates the signature integrity status of a deserialized vault.
/// </summary>
public enum VaultSignatureStatus
{
    /// <summary>No signature field present (legacy vault or externally edited).</summary>
    Missing,

    /// <summary>Signature is present and matches the vault contents.</summary>
    Valid,

    /// <summary>Signature is present but does not match — vault was tampered with or edited outside the app.</summary>
    Invalid,
}

public static class VaultDataService
{
    private static readonly JsonSerializerOptions _loadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions _saveOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions _canonicalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static VaultV2? DeserializeVault(string jsonContent)
    {
        try
        {
            VaultV2? vault = JsonSerializer.Deserialize<VaultV2>(jsonContent, _loadOptions);

            // V2 or newer detected
            if (vault is not null && vault.Value.Version >= 2)
            {
                return vault;
            }
        }
        catch
        {
            // Ignore failure
        }

        return null;
    }

    /// <summary>
    /// Deserializes the vault and verifies its HMAC integrity.
    /// </summary>
    public static async Task<VaultDeserializeResult> DeserializeAndVerifyAsync(
        string jsonContent,
        byte[] masterKey,
        ICryptoService crypto,
        CancellationToken cancellationToken
    )
    {
        VaultV2? vault = DeserializeVault(jsonContent);

        if (vault is null)
        {
            return new VaultDeserializeResult { Vault = null, SignatureStatus = VaultSignatureStatus.Missing };
        }

        VaultSignatureStatus status = await VerifySignatureAsync(vault.Value, masterKey, crypto, cancellationToken);

        return new VaultDeserializeResult { Vault = vault, SignatureStatus = status };
    }

    /// <summary>
    /// Serializes the vault and computes a signature (HMAC-SHA512) over the canonical content.
    /// </summary>
    public static async Task<string> SerializeAndSignAsync(
        VaultV2 vault,
        byte[] masterKey,
        ICryptoService crypto,
        CancellationToken cancellationToken
    )
    {
        // Sort items for deterministic output.
        vault.Items.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

        // Serialize without signature to compute the canonical form.
        VaultV2 unsigned = new() { Version = vault.Version, Items = vault.Items, Signature = null };
        string canonical = JsonSerializer.Serialize(unsigned, _canonicalOptions);

        string signature = await ComputeSignatureAsync(canonical, masterKey, crypto, cancellationToken);

        // Serialize again with the signature included.
        VaultV2 signed = new() { Version = vault.Version, Items = vault.Items, Signature = signature };

        return JsonSerializer.Serialize(signed, _saveOptions);
    }

    private static async Task<VaultSignatureStatus> VerifySignatureAsync(
        VaultV2 vault,
        byte[] masterKey,
        ICryptoService crypto,
        CancellationToken cancellationToken
    )
    {
        if (vault.Signature is null)
        {
            return VaultSignatureStatus.Missing;
        }

        // Re-serialize without signature to get the canonical form.
        VaultV2 unsigned = new() { Version = vault.Version, Items = vault.Items, Signature = null };
        string canonical = JsonSerializer.Serialize(unsigned, _canonicalOptions);

        string expected = await ComputeSignatureAsync(canonical, masterKey, crypto, cancellationToken);

        return string.Equals(vault.Signature, expected, StringComparison.Ordinal)
            ? VaultSignatureStatus.Valid
            : VaultSignatureStatus.Invalid;
    }

    private static async Task<string> ComputeSignatureAsync(
        string content,
        byte[] masterKey,
        ICryptoService crypto,
        CancellationToken cancellationToken
    )
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
        byte[] hmacBytes = await crypto.ComputeHmacSha512Async(data, masterKey, cancellationToken);
        return Base58.Encode(hmacBytes);
    }
}
