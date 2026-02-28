using ItchyPassword.Core.Models;
using System.Text.Json;

namespace ItchyPassword.Core.Services;

public class VaultDataService
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

    public static VaultV2? DeserializeVault(string jsonContent)
    {
        try
        {
            VaultV2? vault = JsonSerializer.Deserialize<VaultV2>(jsonContent, _loadOptions);

            // V2 or newer detected
            if (vault is not null && vault.Version >= 2)
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

    public static string SerializeVault(VaultV2 vault)
    {
        // Sort items by Id for deterministic output across saves.
        vault.Items.Sort((a, b) => a.Id.CompareTo(b.Id));

        return JsonSerializer.Serialize(vault, _saveOptions);
    }
}
