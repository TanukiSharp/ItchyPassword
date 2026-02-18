using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;

namespace ItchyPassword.Client.Services;

public class VaultUnlockService(ClientVaultState state)
{
    /// <summary>
    /// Attempts to unlock the vault using the master key stored in state.
    /// Returns a tuple indicating success and an error message if failed.
    /// </summary>
    public async Task<(bool Success, string Error)> UnlockAsync(Action<string>? onStatusChanged = null)
    {
        if (state.HasMasterKey == false)
        {
             return (false, "Master key not provided.");
        }

        if (state.ReadConnector is null)
        {
             return (false, "No active vault connector selected.");
        }

        // Ensure current connector config is loaded (secrets are decrypted automatically via state).
        await state.ReadConnector.LoadConfigurationAsync();

        if (state.ReadConnector.IsConfigured == false)
        {
            return (false, "Connector not configured.");
        }

        try
        {
            bool isConnected = await state.ReadConnector.ConnectAsync();

            if (isConnected == false)
            {
                 return (false, $"Could not connect to {state.ReadConnector.Name}.");
            }

            string content = await state.ReadConnector.LoadVaultAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                state.Vault = new VaultV2() { Version = 2, Items = [] };
            }
            else
            {
                // Try Load V2
                VaultV2? vault = VaultDataService.DeserializeVault(content);

                if (vault is null)
                {
                    // If V2 load failed, try migration service
                    if (VaultMigrationService.IsLegacyVault(content))
                    {
                        onStatusChanged?.Invoke("Migrating vault...");
                        var migrationProgress = new Progress<double>(percent =>
                            onStatusChanged?.Invoke($"Migrating vault... {percent:f1}%"));
                        vault = await VaultMigrationService.MigrateAsync(content, state.MasterKey, migrationProgress);
                    }
                    else
                    {
                        // Unknown format
                        return (false, "Unknown vault format or password incorrect.");
                    }
                }

                state.Vault = vault ?? new VaultV2 { Version = 2, Items = [] };
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
