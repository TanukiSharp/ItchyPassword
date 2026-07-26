using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ItchyPassword.Core.Services;

public class VaultMigrationService(IVaultCryptoService vaultCrypto, ErrorLogService errorLog)
{
    /// <summary>
    /// Checks if the provided JSON content looks like a Legacy V1 vault.
    /// </summary>
    public static bool IsLegacyVault(string jsonContent)
    {
        try
        {
            // Simple heuristic to avoid full parsing: V1 is an object but NOT a Vault object (no "version" property at root)
            // But V2 has "version": 2.
            // V1 is just key-value pairs of services.
            JsonNode? root = JsonNode.Parse(jsonContent);

            if (root is JsonObject obj)
            {
                // If it has "version" and "items", it's likely V2+
                if (obj.ContainsKey("version") && obj.ContainsKey("items"))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Migrates a V1 vault string to a V2 Vault object, including password-to-cipher conversion.
    /// </summary>
    public async Task<VaultV2> MigrateAsync(string jsonContent, byte[] masterKey, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // 1. Structural Migration (JSON -> Vault Items)
        VaultV2 vault = PerformStructuralMigration(jsonContent);

        // 2. Content Migration (Password Recipes -> Encrypted Ciphers)
        await MigrateLegacyPasswordRecipesToCiphersAsync(vault, masterKey, progress, cancellationToken);
        await MigrateLegacyCiphersAsync(vault, masterKey, progress, cancellationToken);

        vault.Items.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

        return vault;
    }

    private VaultV2 PerformStructuralMigration(string jsonContent)
    {
        var vault = new VaultV2 { Version = 2, Items = [] };

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(jsonContent);
        }
        catch (Exception ex)
        {
            errorLog.Log("Failed to parse legacy vault JSON.", nameof(VaultMigrationService), ex);
            return vault;
        }

        if (root is not JsonObject rootObj)
        {
            return vault;
        }

        foreach (KeyValuePair<string, JsonNode?> kvp in rootObj)
        {
            if (kvp.Value is not JsonObject childObj)
            {
                continue;
            }

            // Isolate each top-level entry: a single malformed service must not abort the
            // migration and silently drop every other entry in the vault.
            try
            {
                TraverseV1Node(childObj, kvp.Key, string.Empty, vault.Items);
            }
            catch (Exception ex)
            {
                errorLog.Log($"Failed to migrate legacy entry '{kvp.Key}'; it was skipped.", nameof(VaultMigrationService), ex);
            }
        }

        return vault;
    }

    private static void TraverseV1Node(JsonObject node, string name, string parentPath, List<VaultItemV2> items)
    {
        bool processedPassword = false;
        bool processedCiphers = false;

        // 1. Check for "password" -> This node is a Password Item
        if (node.TryGetPropertyValue("password", out JsonNode? passNode) && passNode is JsonObject passObj)
        {
            // Strict check: Is this really a password object?
            if (passObj.TryGetPropertyValue("public", out JsonNode? pubPartNode) &&
                pubPartNode?.GetValueKind() == JsonValueKind.String &&
                pubPartNode.ToString().Length >= 4)
            {
                DateTimeOffset legacyDate = ParseLegacyDate(passObj);

                var item = new VaultItemV2
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath} / {name}",
                    Type = VaultItemTypeV2.StaticKey,
                    CreatedAt = legacyDate,
                    LastModified = legacyDate
                };

                int version = ExtractValue($"p: {item.Name}", passObj, "version", StaticKeyDataConstants.LatestCryptoVersion);
                int length = ExtractValue($"p: {item.Name}", passObj, "length", StaticKeyDataConstants.DefaultLength);
                string alphabet = ExtractValue($"p: {item.Name}", passObj, "alphabet", StaticKeyDataConstants.DefaultAlphabet);

                item.SetData(new StaticKeyDataV2
                {
                    PublicPart = pubPartNode.ToString(),
                    CryptoVersion = version,
                    Length = length,
                    Alphabet = alphabet,
                    EncodingVersion = 1,
                });

                // Handle customKeys inside password object.
                if (passObj.TryGetPropertyValue("customKeys", out JsonNode? ck) && ck is JsonObject ckObj)
                {
                    foreach (KeyValuePair<string, JsonNode?> k in ckObj)
                    {
                        item.Metadata ??= [];
                        item.Metadata.Add(new MetadataEntryV2
                        {
                            Key = k.Key,
                            Value = k.Value?.ToString() ?? string.Empty
                        });
                    }
                }

                items.Add(item);
                processedPassword = true;
            }
        }

        // 2. Check for "ciphers" -> This node might generate multiple Cipher Items
        if (node.TryGetPropertyValue("ciphers", out JsonNode? ciphersNode) && ciphersNode is JsonObject ciphersObj)
        {
            // Check if this is a valid ciphers container
            bool looksLikeCiphers = false;

            foreach (KeyValuePair<string, JsonNode?> child in ciphersObj)
            {
                if (child.Value is JsonObject co &&
                    co.ContainsKey("value") &&
                    co.ContainsKey("version")
                )
                {
                    looksLikeCiphers = true;
                    break;
                }
            }

            if (looksLikeCiphers)
            {
                foreach (KeyValuePair<string, JsonNode?> cipherKvp in ciphersObj)
                {
                    if (cipherKvp.Value is JsonObject cipherDetail)
                    {
                        if (cipherDetail.ContainsKey("value") == false)
                        {
                            continue;
                        }

                        string itemName = string.IsNullOrWhiteSpace(cipherKvp.Key) ? name : $"{name} / {cipherKvp.Key}";

                        DateTimeOffset legacyCipherDate = ParseLegacyDate(cipherDetail);

                        var item = new VaultItemV2
                        {
                            Id = Guid.NewGuid(),
                            Name = string.IsNullOrWhiteSpace(parentPath) ? itemName : $"{parentPath} / {itemName}",
                            Type = VaultItemTypeV2.Secret,
                            CreatedAt = legacyCipherDate,
                            LastModified = legacyCipherDate
                        };

                        string cipher = ExtractValue($"c: {item.Name}", cipherDetail, "value", string.Empty);
                        int cipherCryptoVersion = ExtractValue($"c: {item.Name}", cipherDetail, "version", SecretDataConstants.LatestCryptoVersion);
                        string encoding = ExtractValue($"c: {item.Name}", cipherDetail, "encoding", "base62");

                        item.SetData(new SecretDataV2
                        {
                            Cipher = cipher,
                            CryptoVersion = cipherCryptoVersion,
                            Encoding = encoding
                        });
                        if (cipherDetail.TryGetPropertyValue("customKeys", out JsonNode? ck) && ck is JsonObject ckObj)
                        {
                            foreach (KeyValuePair<string, JsonNode?> k in ckObj)
                            {
                                item.Metadata ??= [];
                                item.Metadata.Add(new MetadataEntryV2
                                {
                                    Key = k.Key,
                                    Value = k.Value?.ToString() ?? string.Empty
                                });
                            }
                        }

                        items.Add(item);
                    }
                }

                processedCiphers = true;
            }
        }

        string newPath = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath} / {name}";

        foreach (KeyValuePair<string, JsonNode?> kvp in node)
        {
            if (kvp.Key == "password" && processedPassword ||
                kvp.Key == "ciphers" && processedCiphers ||
                kvp.Key == "customKeys")
            {
                continue;
            }

            if (kvp.Value is JsonObject childNode)
            {
                TraverseV1Node(childNode, kvp.Key, newPath, items);
            }
        }
    }

    private async Task MigrateLegacyPasswordRecipesToCiphersAsync(VaultV2 vault, byte[] masterKeyBytes, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // Pre-filter items that need migration to avoid repeated checks in loop.
        List<VaultItemV2> itemsToMigrate = vault.Items.Where(item =>
            item.Type == VaultItemTypeV2.StaticKey &&
            item.StaticKeyData is not null &&
            string.IsNullOrWhiteSpace(item.StaticKeyData.Value.PublicPart) == false &&
            item.StaticKeyData.Value.PublicPart.Length > 20 &&
            item.StaticKeyData.Value.PublicPart.All(c => Base62.Alphabet.Contains(c))
        ).ToList();

        int total = itemsToMigrate.Count;
        int completed = 0;

        foreach (VaultItemV2 item in itemsToMigrate)
        {
            try
            {
                string staticKey = await vaultCrypto.GenerateStaticKeyAsync(item.StaticKeyData!.Value, masterKeyBytes, cancellationToken);
                SecretDataV2 secret = await vaultCrypto.EncryptSecretAsync(staticKey, masterKeyBytes, SecretDataConstants.LatestEncoding, cancellationToken);

                item.Type = VaultItemTypeV2.Secret;
                item.SetData(secret);
            }
            catch (Exception ex)
            {
                errorLog.Log(
                    $"Failed to migrate password entry '{item.Name}' to cipher.",
                    nameof(VaultMigrationService),
                    ex
                );
            }
            finally
            {
                completed++;
                progress?.Report(completed * 100.0 / total);
            }
        }
    }

    private async Task MigrateLegacyCiphersAsync(VaultV2 vault, byte[] masterKeyBytes, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // Pre-filter items that need migration to avoid repeated checks in loop.
        List<VaultItemV2> itemsToMigrate = vault.Items.Where(item =>
            item.Type == VaultItemTypeV2.Secret &&
            item.SecretData is not null &&
            string.IsNullOrWhiteSpace(item.SecretData!.Value.Cipher) == false &&
            (item.SecretData.Value.CryptoVersion != SecretDataConstants.LatestCryptoVersion || item.SecretData.Value.Encoding != SecretDataConstants.LatestEncoding)
        ).ToList();

        int total = itemsToMigrate.Count;
        int completed = 0;

        foreach (VaultItemV2 item in itemsToMigrate)
        {
            try
            {
                SecretDataV2 secretData = item.SecretData!.Value;

                string decryptedValue = await vaultCrypto.DecryptSecretAsync(secretData, masterKeyBytes, cancellationToken);

                secretData = await vaultCrypto.EncryptSecretAsync(decryptedValue, masterKeyBytes, SecretDataConstants.LatestEncoding, cancellationToken);

                item.SetData(secretData);
            }
            catch (Exception ex)
            {
                errorLog.Log(
                    $"Failed to re-encrypt cipher '{item.Name}'.",
                    nameof(VaultMigrationService),
                    ex
                );
            }
            finally
            {
                completed++;
                progress?.Report(completed * 100.0 / total);
            }
        }
    }

    /// <summary>
    /// Reads a strongly-typed value from a legacy node, tolerating type mismatches.
    /// A value stored with an unexpected JSON type (for example a number serialized as a
    /// string in an old export or a hand-edited vault) is coerced when possible and falls
    /// back to <paramref name="defaultValue"/> otherwise. This method never throws, so a
    /// single oddly-typed field cannot abort the migration of the surrounding vault.
    /// </summary>
    private static T ExtractValue<T>(string parentPath, JsonObject obj, string key, T defaultValue)
    {
        if (obj.TryGetPropertyValue(key, out JsonNode? valNode) == false || valNode is null)
        {
            return defaultValue;
        }

        // Fast path: the JSON type matches the requested type.
        try
        {
            if (valNode.GetValue<T?>() is T value)
            {
                return value;
            }
        }
        catch (Exception)
        {
            // Type mismatch (e.g. "version": "3"). Fall through to coercion below.
        }

        // Coercion path: recover common cross-type mismatches instead of losing the value.
        string raw = valNode.ToString();

        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt))
            {
                return (T)(object)parsedInt;
            }
        }
        else if (typeof(T) == typeof(string))
        {
            return (T)(object)raw;
        }

        return defaultValue;
    }

    /// <summary>
    /// Parses the optional "datetime" field of a legacy node, falling back to the current
    /// time (never <see cref="DateTimeOffset.MinValue"/>) when it is missing or unparseable.
    /// Uses invariant culture so import behaves identically regardless of the machine locale.
    /// </summary>
    private static DateTimeOffset ParseLegacyDate(JsonObject node)
    {
        if (node.TryGetPropertyValue("datetime", out JsonNode? dt) &&
            dt is not null &&
            DateTimeOffset.TryParse(dt.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }
}
