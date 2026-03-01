using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ItchyPassword.Core.Services;

public class VaultMigrationService(ICryptoService crypto)
{
    private static readonly HashSet<char> Base62Chars = new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
    private const int DefaultLength = 64;

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
    public async Task<VaultV2> MigrateAsync(string jsonContent, byte[] masterKey, IProgress<double>? progress = null)
    {
        // 1. Structural Migration (JSON -> Vault Items)
        VaultV2 vault = PerformStructuralMigration(jsonContent);

        // 2. Content Migration (Password Recipes -> Encrypted Ciphers)
        await MigrateLegacyPasswordRecipesToCiphersAsync(vault, masterKey, progress);

        return vault;
    }

    private static VaultV2 PerformStructuralMigration(string jsonContent)
    {
        var vault = new VaultV2 { Version = 2, Items = [] };

        try
        {
            JsonNode? root = JsonNode.Parse(jsonContent);

            if (root is JsonObject rootObj)
            {
                foreach (KeyValuePair<string, JsonNode?> kvp in rootObj)
                {
                    if (kvp.Value is JsonObject childObj)
                    {
                        TraverseV1Node(childObj, kvp.Key, string.Empty, vault.Items);
                    }
                }
            }
        }
        catch (Exception)
        {
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
                DateTimeOffset legacyDate = DateTimeOffset.UtcNow;
                if (passObj.TryGetPropertyValue("datetime", out JsonNode? dt) && DateTimeOffset.TryParse(dt?.ToString(), out legacyDate))
                {
                    // Use the original datetime from the legacy vault.
                }

                var item = new VaultItemV2
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath} / {name}",
                    Type = VaultItemTypeV2.StaticKey,
                    CreatedAt = legacyDate,
                    LastModified = legacyDate
                };

                int version = ExtractValue($"p: {item.Name}", passObj, "version", 2);
                int length = ExtractValue($"p: {item.Name}", passObj, "length", 64);
                string alphabet = ExtractValue($"p: {item.Name}", passObj, "alphabet", StaticKeyDataV2.DefaultAlphabet);

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

                        DateTimeOffset legacyCipherDate = DateTimeOffset.UtcNow;
                        if (cipherDetail.TryGetPropertyValue("datetime", out JsonNode? dt) && DateTimeOffset.TryParse(dt?.ToString(), out legacyCipherDate))
                        {
                            // Use the original datetime from the legacy vault.
                        }

                        var item = new VaultItemV2
                        {
                            Id = Guid.NewGuid(),
                            Name = string.IsNullOrWhiteSpace(parentPath) ? itemName : $"{parentPath} / {itemName}",
                            Type = VaultItemTypeV2.Secret,
                            CreatedAt = legacyCipherDate,
                            LastModified = legacyCipherDate
                        };

                        string cipher = ExtractValue($"c: {item.Name}", cipherDetail, "value", string.Empty);
                        int cipherCryptoVersion = ExtractValue($"c: {item.Name}", cipherDetail, "version", 3);
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

    private async Task MigrateLegacyPasswordRecipesToCiphersAsync(VaultV2 vault, byte[] masterKeyBytes, IProgress<double>? progress = null)
    {

        // Pre-filter items that need migration to avoid repeated checks in loop.
        List<VaultItemV2> itemsToMigrate = vault.Items.Where(item =>
            item.Type == VaultItemTypeV2.StaticKey &&
            item.StaticKeyData is not null &&
            string.IsNullOrWhiteSpace(item.StaticKeyData.PublicPart) == false &&
            item.StaticKeyData.PublicPart.Length > 20 &&
            item.StaticKeyData.PublicPart.All(c => Base62Chars.Contains(c))
        ).ToList();

        int total = itemsToMigrate.Count;
        int completed = 0;

        foreach (VaultItemV2 item in itemsToMigrate)
        {
            try
            {
                StaticKeyDataV2 skParams = item.StaticKeyData!;
                string publicPart = skParams.PublicPart;
                int cryptoVersion = skParams.CryptoVersion;

                if (cryptoVersion != 1 && cryptoVersion != 2)
                {
                    continue;
                }

                string alphabet = string.IsNullOrWhiteSpace(skParams.Alphabet) ? StaticKeyDataV2.DefaultAlphabet : skParams.Alphabet;
                int length = skParams.Length > 0 ? skParams.Length : DefaultLength;

                byte[] publicBytes = System.Text.Encoding.UTF8.GetBytes(publicPart);
                byte[] derivedBytes;

                if (cryptoVersion == 1)
                {
                    derivedBytes = await crypto.GeneratePasswordV1Async(masterKeyBytes, publicBytes);
                }
                else
                {
                    derivedBytes = await crypto.GeneratePasswordV2Async(masterKeyBytes, publicBytes, "Password");
                }

                string password = BaseN.EncodeOneWay(derivedBytes, alphabet);

                if (password.Length > length)
                {
                    password = password[..length];
                }

                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] encryptedBlob = await crypto.EncryptV3Async(passwordBytes, masterKeyBytes);

                item.Type = VaultItemTypeV2.Secret;
                item.SetData(new SecretDataV2
                {
                    Cipher = Base58.Encode(encryptedBlob),
                    CryptoVersion = 3,
                    Encoding = "base58"
                });
            }
            catch (Exception)
            {
            }
            finally
            {
                completed++;
                progress?.Report(completed * 100.0 / total);
            }
        }
    }

    private static T ExtractValue<T>(string parentPath, JsonObject obj, string key, T defaultValue)
    {
        if (obj.TryGetPropertyValue(key, out JsonNode? valNode) == false)
        {
            return defaultValue;
        }

        if (valNode is null)
        {
            return defaultValue;
        }

        T? nullableValue = valNode.GetValue<T?>();

        if (nullableValue is null)
        {
            return defaultValue;
        }

        return nullableValue;
    }
}
