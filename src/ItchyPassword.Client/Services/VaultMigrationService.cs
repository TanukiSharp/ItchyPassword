using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Helpers;

namespace ItchyPassword.Client.Services;

public class VaultMigrationService(ICryptoService crypto)
{
    // Static readonly for reuse across all calls.
    private static readonly HashSet<char> Base62Chars = new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
    private const string DefaultAlphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~";
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
            var root = JsonNode.Parse(jsonContent);
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
    public async Task<Vault> MigrateAsync(string jsonContent, string masterKey, IProgress<double>? progress = null)
    {
        // 1. Structural Migration (JSON -> Vault Items)
        var vault = PerformStructuralMigration(jsonContent);

        // 2. Content Migration (Password Recipes -> Encrypted Ciphers)
        await MigrateLegacyPasswordRecipesToCiphersAsync(vault, masterKey, progress);

        return vault;
    }

    private static Vault PerformStructuralMigration(string jsonContent)
    {
        var vault = new Vault { Version = 2, Items = [] };

        try
        {
            var root = JsonNode.Parse(jsonContent);
            if (root is JsonObject rootObj)
            {
                foreach (var kvp in rootObj)
                {
                    if (kvp.Value is JsonObject childObj)
                    {
                        TraverseV1Node(childObj, kvp.Key, "", vault.Items);
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

    private static void TraverseV1Node(JsonObject node, string name, string parentPath, List<VaultItem> items)
    {
        bool processedPassword = false;
        bool processedCiphers = false;

        // 1. Check for "password" -> This node is a Password Item
        if (node.TryGetPropertyValue("password", out var passNode) && passNode is JsonObject passObj)
        {
            // Strict check: Is this really a password object?
            if (passObj.TryGetPropertyValue("public", out var pubPartNode) &&
                pubPartNode?.GetValueKind() == JsonValueKind.String &&
                pubPartNode.ToString().Length >= 4)
            {
                var item = new VaultItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"{parentPath} / {name}",
                    Type = VaultItemType.Password,
                    LastModified = DateTime.UtcNow
                };

                item.Parameters["public"] = pubPartNode.ToString();

                if (passObj.TryGetPropertyValue("version", out var ver))
                    item.Parameters["version"] = ver?.ToString() ?? "2";

                if (passObj.TryGetPropertyValue("length", out var len))
                    item.Parameters["length"] = len?.ToString() ?? "64";

                if (passObj.TryGetPropertyValue("alphabet", out var alpha))
                    item.Parameters["alphabet"] = alpha?.ToString() ?? "";

                if (passObj.TryGetPropertyValue("datetime", out var dt) && DateTime.TryParse(dt?.ToString(), out var date))
                    item.LastModified = date;

                // Handle customKeys inside password object
                if (passObj.TryGetPropertyValue("customKeys", out var ck) && ck is JsonObject ckObj)
                {
                    foreach(var k in ckObj)
                    {
                        item.Metadata ??= [];
                        item.Metadata[k.Key] = k.Value?.ToString() ?? string.Empty;
                    }
                }

                items.Add(item);
                processedPassword = true;
            }
        }

        // 2. Check for "ciphers" -> This node might generate multiple Cipher Items
        if (node.TryGetPropertyValue("ciphers", out var ciphersNode) && ciphersNode is JsonObject ciphersObj)
        {
            // Check if this is a valid ciphers container
            bool looksLikeCiphers = false;
            foreach (var child in ciphersObj)
            {
                if (child.Value is JsonObject co &&
                    co.ContainsKey("value") &&
                    co.ContainsKey("version"))
                {
                    looksLikeCiphers = true;
                    break;
                }
            }

            if (looksLikeCiphers)
            {
                foreach (var cipherKvp in ciphersObj)
                {
                    if (cipherKvp.Value is JsonObject cipherDetail)
                    {
                        if (!cipherDetail.ContainsKey("value")) continue;

                        var itemName = string.IsNullOrWhiteSpace(cipherKvp.Key) ? name : $"{name} - {cipherKvp.Key}";

                        var item = new VaultItem
                        {
                            Id = Guid.NewGuid(),
                            Name = $"{parentPath} / {itemName}",
                            Type = VaultItemType.Cipher,
                            LastModified = DateTime.UtcNow
                        };

                        if (cipherDetail.TryGetPropertyValue("value", out var val))
                            item.Content = val?.ToString() ?? string.Empty;

                        if (cipherDetail.TryGetPropertyValue("version", out var ver))
                            item.Parameters["version"] = ver?.GetValue<int?>() ?? 3;

                        if (cipherDetail.TryGetPropertyValue("encoding", out var enc))
                            item.Parameters["encoding"] = enc?.ToString() ?? "base62";

                        if (cipherDetail.TryGetPropertyValue("datetime", out var dt) && DateTimeOffset.TryParse(dt?.ToString(), out DateTimeOffset date))
                            item.LastModified = date;

                        if (cipherDetail.TryGetPropertyValue("customKeys", out var ck) && ck is JsonObject ckObj)
                        {
                            foreach(var k in ckObj)
                            {
                                item.Parameters[$"custom:{k.Key}"] = k.Value?.ToString() ?? "";
                            }
                        }

                        items.Add(item);
                    }
                }
                processedCiphers = true;
            }
        }

        string newPath = string.IsNullOrWhiteSpace(parentPath) ? name : $"{parentPath} / {name}";

        foreach (var kvp in node)
        {
            if (kvp.Key == "password" && processedPassword) continue;
            if (kvp.Key == "ciphers" && processedCiphers) continue;
            if (kvp.Key == "customKeys") continue;

            if (kvp.Value is JsonObject childNode)
            {
                TraverseV1Node(childNode, kvp.Key, newPath, items);
            }
        }
    }

    private async Task MigrateLegacyPasswordRecipesToCiphersAsync(Vault vault, string masterKey, IProgress<double>? progress = null)
    {
        var masterKeyBytes = Encoding.UTF8.GetBytes(masterKey);

        // Pre-filter items that need migration to avoid repeated checks in loop
        List<VaultItem> itemsToMigrate = vault.Items.Where(item =>
            item.Type == VaultItemType.Password &&
            item.Parameters.TryGetValue("public", out object? publicPart) &&
            publicPart is string publicPartStr &&
            string.IsNullOrWhiteSpace(publicPartStr) == false &&
            publicPartStr.Length > 20 &&
            publicPartStr.All(c => Base62Chars.Contains(c))
        ).ToList();

        int total = itemsToMigrate.Count;
        int completed = 0;

        foreach (var item in itemsToMigrate)
        {
            try
            {
                string? publicPart = item.Parameters["public"] as string;
                int version = item.Parameters.TryGetValue("version", out object? v) && v is int vInt ? vInt : 2;

                if (publicPart is null || (version != 1 && version != 2))
                {
                    continue;
                }

                string alphabet = item.Parameters.TryGetValue("alphabet", out object? alpha) && alpha is string alphaStr ? alphaStr : DefaultAlphabet;
                int length = (item.Parameters.TryGetValue("length", out object? len) && len is int lenInt) ? lenInt : DefaultLength;

                byte[] publicBytes = Encoding.UTF8.GetBytes(publicPart);
                byte[] derivedBytes;

                if (version == 1)
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

                item.Type = VaultItemType.Cipher;
                item.Content = Convert.ToBase64String(encryptedBlob);

                item.Parameters.Remove("public");
                item.Parameters.Remove("alphabet");
                item.Parameters.Remove("length");

                item.Parameters["version"] = 3;
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
