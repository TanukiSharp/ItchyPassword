using System.Text.Json;
using System.Text.Json.Nodes;
using ItchyPassword.Core.Models;

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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Vault? LoadVault(string jsonContent)
    {
        try
        {
            var vault = JsonSerializer.Deserialize<Vault>(jsonContent, _loadOptions);

            // V2 or newer detected
            if (vault != null && vault.Version >= 2)
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

    public string SerializeVault(Vault vault)
    {
        // TODO: Implement sorting logic here using JsonDocument/Node manipulation if standard serializer doesn't support property sorting.
        // Or implement custom Converter.
        // For now standard serialization.

        return JsonSerializer.Serialize(vault, _saveOptions);
    }
}
