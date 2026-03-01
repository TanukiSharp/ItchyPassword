using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Services;

public interface IVaultCryptoService
{
    Task<string> DecryptSecretAsync(SecretDataV2 secretData, byte[] masterKey);
    Task<SecretDataV2> EncryptSecretAsync(string plaintext, byte[] masterKey, string encoding = "base58");
    Task<string> GenerateStaticKeyAsync(StaticKeyDataV2 data, byte[] masterKey);
}

/// <summary>
/// Handles item-level encryption, decryption, and key derivation logic for vault items.
/// Abstracts underlying cryptographic primitives and encoding schemes.
/// </summary>
public class VaultCryptoService(ICryptoService cryptoService) : IVaultCryptoService
{
    private const string DefaultEncoding = "base58";
    private const int DefaultCryptoVersion = 3;

    /// <summary>
    /// Decrypts a Secret-type vault item.
    /// </summary>
    public async Task<string> DecryptSecretAsync(SecretDataV2 secretData, byte[] masterKey)
    {
        if (secretData is null || string.IsNullOrWhiteSpace(secretData.Cipher))
        {
             throw new InvalidOperationException("No secret data to decrypt.");
        }
        if (masterKey is null || masterKey.Length == 0)
        {
             throw new ArgumentException("Key cannot be empty.", nameof(masterKey));
        }

        byte[] encryptedBytes = DecodeString(secretData.Cipher, secretData.Encoding);

        byte[] decryptedBytes = secretData.CryptoVersion switch
        {
            3 => await cryptoService.DecryptV3Async(encryptedBytes, masterKey),
            _ => throw new NotSupportedException($"Crypto version {secretData.CryptoVersion} is not supported.")
        };

        return System.Text.Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Encrypts a plaintext string into a SecretDataV2 object.
    /// </summary>
    public async Task<SecretDataV2> EncryptSecretAsync(string plaintext, byte[] masterKey, string encoding = DefaultEncoding)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
             throw new ArgumentException("Plaintext cannot be empty.", nameof(plaintext));
        }
        if (masterKey is null || masterKey.Length == 0)
        {
             throw new ArgumentException("Key cannot be empty.", nameof(masterKey));
        }

        byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);

        // Always encrypt with the latest version (v3).
        byte[] encrypted = await cryptoService.EncryptV3Async(plaintextBytes, masterKey);

        return new SecretDataV2
        {
            Cipher = EncodeBytes(encrypted, encoding),
            CryptoVersion = DefaultCryptoVersion,
            Encoding = encoding
        };
    }

    /// <summary>
    /// Generates a static password based on the master key and item configuration.
    /// </summary>
    public async Task<string> GenerateStaticKeyAsync(StaticKeyDataV2 keyData, byte[] masterKey)
    {
        if (keyData is null)
        {
            throw new InvalidOperationException("No static key data.");
        }

        byte[] publicPart = System.Text.Encoding.UTF8.GetBytes(keyData.PublicPart);

        byte[] rawBytes = keyData.CryptoVersion switch
        {
            1 => await cryptoService.GeneratePasswordV1Async(masterKey, publicPart),
            2 => await cryptoService.GeneratePasswordV2Async(masterKey, publicPart),
            _ => throw new NotSupportedException($"Crypto version {keyData.CryptoVersion} is not supported.")
        };

        string result = keyData.EncodingVersion switch
        {
            1 => BaseN.EncodeOneWay(rawBytes, keyData.Alphabet),
            2 => BaseN.Encode(rawBytes, keyData.Alphabet),
            _ => throw new NotSupportedException($"Encoding version {keyData.EncodingVersion} is not supported.")
        };

        if (result.Length > keyData.Length)
        {
            result = result[..keyData.Length];
        }

        return result;
    }

    private static string EncodeBytes(byte[] data, string encoding)
    {
        return encoding switch
        {
            "base58" => Base58.Encode(data),
            "base62" => Base62.Encode(data),
            "base64" => Convert.ToBase64String(data),
            _ => Base58.Encode(data)
        };
    }

    private static byte[] DecodeString(string data, string encoding)
    {
        return encoding switch
        {
            "base58" => Base58.Decode(data),
            "base62" => BaseN.Decode(data, Base62.Alphabet),
            "base64" => Convert.FromBase64String(data),
            _ => Base58.Decode(data)
        };
    }
}
