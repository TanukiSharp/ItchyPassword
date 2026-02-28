using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.Text;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Provides shared encryption, decryption, and configuration persistence helpers for vault connectors.
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

    /// <summary>
    /// Loads all persistable configuration entries from localStorage into their in-memory values.
    /// Entries marked as encrypted are decrypted using the master key when available.
    /// </summary>
    /// <param name="entries">The configuration entries to load.</param>
    /// <param name="storage">The localStorage service.</param>
    /// <param name="masterKey">The current master key, or <c>null</c> if unavailable.</param>
    /// <param name="crypto">The crypto service for decryption.</param>
    public static async Task LoadEntriesAsync(
        IReadOnlyList<ConfigurationEntry> entries,
        LocalStorageService storage,
        string? masterKey,
        ICryptoService crypto)
    {
        foreach (ConfigurationEntry entry in entries)
        {
            if (entry.StorageKey is null)
            {
                continue;
            }

            string? stored = await storage.GetItemAsync(entry.StorageKey);

            if (string.IsNullOrWhiteSpace(stored))
            {
                continue;
            }

            if (entry.IsEncrypted && string.IsNullOrWhiteSpace(masterKey) == false)
            {
                try
                {
                    entry.Value = await DecryptIfNeededAsync(stored, masterKey, crypto);
                }
                catch
                {
                    // Decryption failed — leave value as-is (empty or default).
                    // This can happen when the master key is incorrect.
                }
            }
            else
            {
                entry.Value = stored;
            }
        }
    }

    /// <summary>
    /// Persists all persistable configuration entries to localStorage.
    /// Entries marked as encrypted are encrypted using the master key when available.
    /// </summary>
    /// <param name="entries">The configuration entries to save.</param>
    /// <param name="storage">The localStorage service.</param>
    /// <param name="masterKey">The current master key, or <c>null</c> if unavailable.</param>
    /// <param name="crypto">The crypto service for encryption.</param>
    public static async Task SaveEntriesAsync(
        IReadOnlyList<ConfigurationEntry> entries,
        LocalStorageService storage,
        string? masterKey,
        ICryptoService crypto)
    {
        foreach (ConfigurationEntry entry in entries)
        {
            if (entry.StorageKey is null)
            {
                continue;
            }

            string valueToStore = entry.Value;

            if (entry.IsEncrypted)
            {
                // Never write an encrypted entry in plaintext.
                // Skip entirely when the master key is unavailable.
                if (string.IsNullOrWhiteSpace(masterKey))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(valueToStore) == false)
                {
                    valueToStore = await EncryptAsync(valueToStore, masterKey, crypto);
                }
            }

            await storage.SetItemAsync(entry.StorageKey, valueToStore);
        }
    }

    /// <summary>
    /// Returns the current value of a configuration entry by key.
    /// Falls back to <see cref="ConfigurationEntry.DefaultValue"/> if the value is empty.
    /// </summary>
    /// <param name="entries">The list of configuration entries to search.</param>
    /// <param name="key">The key of the entry to look up.</param>
    /// <returns>The entry's value, or its default value if empty.</returns>
    public static string GetValue(IReadOnlyList<ConfigurationEntry> entries, string key)
    {
        foreach (ConfigurationEntry entry in entries)
        {
            if (entry.Key == key)
            {
                return string.IsNullOrWhiteSpace(entry.Value) ? entry.DefaultValue : entry.Value;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Sets the current value of a configuration entry by key.
    /// </summary>
    /// <param name="entries">The list of configuration entries to search.</param>
    /// <param name="key">The key of the entry to update.</param>
    /// <param name="value">The new value.</param>
    public static void SetValue(IReadOnlyList<ConfigurationEntry> entries, string key, string value)
    {
        foreach (ConfigurationEntry entry in entries)
        {
            if (entry.Key == key)
            {
                entry.Value = value;
                return;
            }
        }
    }

    /// <summary>
    /// Determines whether all required and visible configuration entries have non-empty values.
    /// Respects conditional visibility: an entry whose visibility condition is not met is skipped.
    /// </summary>
    /// <param name="entries">The list of configuration entries to check.</param>
    /// <returns><c>true</c> if all visible required entries have values; otherwise, <c>false</c>.</returns>
    public static bool AreRequiredEntriesFilled(IReadOnlyList<ConfigurationEntry> entries)
    {
        foreach (ConfigurationEntry entry in entries)
        {
            if (entry.IsRequired == false)
            {
                continue;
            }

            // Skip entries hidden by conditional visibility.
            if (IsEntryVisible(entries, entry) == false)
            {
                continue;
            }

            string value = string.IsNullOrWhiteSpace(entry.Value) ? entry.DefaultValue : entry.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a configuration entry is currently visible based on its
    /// <see cref="ConfigurationEntry.VisibleWhenKey"/> and <see cref="ConfigurationEntry.VisibleWhenValue"/> rules.
    /// </summary>
    /// <param name="entries">All configuration entries (to look up the controlling entry).</param>
    /// <param name="entry">The entry whose visibility to check.</param>
    /// <returns><c>true</c> if the entry should be displayed; otherwise, <c>false</c>.</returns>
    public static bool IsEntryVisible(IReadOnlyList<ConfigurationEntry> entries, ConfigurationEntry entry)
    {
        if (entry.VisibleWhenKey is null)
        {
            return true;
        }

        string controllingValue = GetValue(entries, entry.VisibleWhenKey);
        return string.Equals(controllingValue, entry.VisibleWhenValue, StringComparison.Ordinal);
    }
}
