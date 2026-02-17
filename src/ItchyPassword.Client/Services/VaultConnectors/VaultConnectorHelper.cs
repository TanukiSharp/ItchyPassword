using ItchyPassword.Core.Helpers;
using ItchyPassword.Core.Services;
using System.Text;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Provides shared encryption and decryption helpers for vault connector secret values.
/// Secrets are encrypted with the master key using EncryptV3, then base58-encoded with an "enc:" prefix.
/// </summary>
internal static class VaultConnectorHelper
{
    private const string EncryptedPrefix = "enc:";

    /// <summary>
    /// Encrypts a plaintext secret and returns a prefixed base58-encoded string.
    /// </summary>
    /// <param name="plaintext">The plaintext value to encrypt.</param>
    /// <param name="masterKey">The master key used as encryption password.</param>
    /// <param name="crypto">The crypto service.</param>
    /// <returns>The encrypted, base58-encoded value with the "enc:" prefix.</returns>
    public static async Task<string> EncryptAsync(string plaintext, string masterKey, ICryptoService crypto)
    {
        byte[] input = Encoding.UTF8.GetBytes(plaintext);
        byte[] password = Encoding.UTF8.GetBytes(masterKey);
        byte[] encrypted = await crypto.EncryptV3Async(input, password);
        return EncryptedPrefix + Base58.Encode(encrypted);
    }

    /// <summary>
    /// Decrypts a stored value if it carries the "enc:" prefix, otherwise returns it unchanged.
    /// This provides backward compatibility with plaintext values stored before encryption was enabled.
    /// </summary>
    /// <param name="stored">The stored configuration value.</param>
    /// <param name="masterKey">The master key used as decryption password.</param>
    /// <param name="crypto">The crypto service.</param>
    /// <returns>The decrypted plaintext value.</returns>
    public static async Task<string> DecryptIfNeededAsync(string stored, string masterKey, ICryptoService crypto)
    {
        if (stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal) == false)
        {
            return stored;
        }

        string encoded = stored[EncryptedPrefix.Length..];
        byte[] encrypted = Base58.Decode(encoded);
        byte[] password = Encoding.UTF8.GetBytes(masterKey);
        byte[] decrypted = await crypto.DecryptV3Async(encrypted, password);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static async Task BindMemoryToStorageAsync(ConfigStorageKey key, Dictionary<string, string> config, LocalStorageService storage)
    {
        if (config.TryGetValue(key.Config, out string? value))
        {
            await storage.SetItemAsync(key.Storage, value);
        }
    }

    public static async Task BindMemoryToStorageAsync(ConfigStorageKey key, Dictionary<string, string> config, LocalStorageService storage, Func<string, Task<string?>> getter)
    {
        if (config.TryGetValue(key.Config, out string? value) && string.IsNullOrWhiteSpace(value) == false)
        {
            string? newValue = await getter(value);

            if (newValue is not null)
            {
                await storage.SetItemAsync(key.Storage, newValue);
            }
        }
    }

    public static async Task BindStorageToMemoryAsync(ConfigStorageKey key, LocalStorageService storage, Dictionary<string, string> config)
    {
        string? value = await storage.GetItemAsync(key.Storage);

        if (string.IsNullOrWhiteSpace(value) == false)
        {
            config[key.Config] = value;
        }
    }

    public static async Task BindStorageToMemoryAsync(ConfigStorageKey key, LocalStorageService storage, Dictionary<string, string> config, Func<string, Task<string>> getter)
    {
        string? value = await storage.GetItemAsync(key.Storage);

        if (string.IsNullOrWhiteSpace(value) == false)
        {
            config[key.Config] = await getter(value);
        }
    }
}
