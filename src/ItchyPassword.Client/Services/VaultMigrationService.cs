using ItchyPassword.Core.Helpers;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ItchyPassword.Client.Services;

public class VaultMigrationService(ICryptoService crypto)
{
    public const string DefaultAlphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~";

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
    public static async Task<VaultV2> MigrateAsync(string jsonContent, string masterKey, IProgress<double>? progress = null)
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
        catch (Exception ex)
        {
            Console.WriteLine($"Structural Migration Failed: {ex.Message}");
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
                var item = new VaultItemV2
                {
                    Id = Guid.NewGuid(),
                    Name = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath} / {name}",
                    Type = VaultItemTypeV2.StaticKey,
                    LastModified = DateTime.UtcNow
                };

                int cryptoVersion = passObj.TryGetPropertyValue("version", out JsonNode? ver)
                    ? ver?.GetValue<int?>() ?? 2
                    : 2;
                int length = passObj.TryGetPropertyValue("length", out JsonNode? len)
                    ? len?.GetValue<int?>() ?? 64
                    : 64;
                string alphabet = passObj.TryGetPropertyValue("alphabet", out JsonNode? alpha)
                    ? alpha?.ToString() ?? string.Empty
                    : string.Empty;

                item.SetData(new StaticKeyDataV2
                {
                    PublicPart = pubPartNode.ToString(),
                    CryptoVersion = cryptoVersion,
                    Length = length,
                    Alphabet = alphabet
                });
                if (passObj.TryGetPropertyValue("datetime", out JsonNode? dt) && DateTimeOffset.TryParse(dt?.ToString(), out DateTimeOffset date))
                {
                    item.LastModified = date;
                }

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

                        var item = new VaultItemV2
                        {
                            Id = Guid.NewGuid(),
                            Name = string.IsNullOrWhiteSpace(parentPath) ? itemName : $"{parentPath} / {itemName}",
                            Type = VaultItemTypeV2.Secret,
                            LastModified = DateTime.UtcNow
                        };

                        string cipher = cipherDetail.TryGetPropertyValue("value", out JsonNode? val)
                            ? val?.ToString() ?? string.Empty
                            : string.Empty;
                        int cipherCryptoVersion = cipherDetail.TryGetPropertyValue("version", out JsonNode? ver)
                            ? ver?.GetValue<int?>() ?? 3
                            : 3;
                        string encoding = cipherDetail.TryGetPropertyValue("encoding", out JsonNode? enc)
                            ? enc?.ToString() ?? "base62"
                            : "base62";

                        item.SetData(new SecretDataV2
                        {
                            Cipher = cipher,
                            CryptoVersion = cipherCryptoVersion,
                            Encoding = encoding
                        });
                        if (cipherDetail.TryGetPropertyValue("datetime", out JsonNode? dt) && DateTimeOffset.TryParse(dt?.ToString(), out DateTimeOffset date))
                        {
                            item.LastModified = date;
                        }
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

    private async Task MigrateLegacyPasswordRecipesToCiphersAsync(VaultV2 vault, string masterKey, IProgress<double>? progress = null)
    {
        byte[] masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);

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

                string alphabet = string.IsNullOrWhiteSpace(skParams.Alphabet) ? DefaultAlphabet : skParams.Alphabet;
                int length = skParams.Length > 0 ? skParams.Length : DefaultLength;

                byte[] publicBytes = Encoding.UTF8.GetBytes(publicPart);
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

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] encryptedBlob = await crypto.EncryptV3Async(passwordBytes, masterKeyBytes);

                item.Type = VaultItemTypeV2.Secret;
                item.SetData(new SecretDataV2
                {
                    Cipher = Base58.Encode(encryptedBlob),
                    CryptoVersion = 3,
                    Encoding = "base58"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to migrate item {item.Name}: {ex.Message}");
            }
            finally
            {
                completed++;
                progress?.Report(completed * 100.0 / total);
            }
        }
    }
}
