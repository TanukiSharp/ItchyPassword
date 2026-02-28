using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;

namespace ItchyPassword.Client.Services;

public class VaultUnlockService(VaultSession session, IMasterKeyProvider masterKeyProvider, VaultMigrationService vaultMigrationService)
{
    /// <summary>
    /// Attempts to unlock the vault using the master key from the provider.
    /// Returns a tuple indicating success and an error message if failed.
    /// </summary>
    public async Task<(bool Success, string Error)> UnlockAsync(Action<string>? onStatusChanged = null)
    {
        if (masterKeyProvider.HasMasterKey == false)
        {
             return (false, "Master key not provided.");
        }

        if (session.ReadConnector is null)
        {
             return (false, "No active vault connector selected.");
        }

        try
        {
            // Ensure current connector config is loaded (secrets are decrypted with the master key).
            await session.ReadConnector.LoadConfigurationAsync();
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        if (session.ReadConnector.IsConfigured == false)
        {
            return (false, "Connector not configured.");
        }

        try
        {
            bool hasAccess = await session.ReadConnector.AccessAsync();

            if (hasAccess == false)
            {
                 string errorMessage = session.ReadConnector.AccessFailureMessage
                     ?? $"Could not access {session.ReadConnector.Name}.";
                 return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        try
        {
            string content = await session.ReadConnector.LoadVaultAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                session.Vault = new VaultV2() { Version = 2, Items = [] };
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
                        var migrationProgress = new Progress<double>(percent => onStatusChanged?.Invoke($"Migrating vault... {percent:f1}%"));
                        vault = await vaultMigrationService.MigrateAsync(content, masterKeyProvider.MasterKey, migrationProgress);
                    }
                    else
                    {
                        // Unknown format
                        return (false, "Unknown vault format or password incorrect.");
                    }
                }

                session.Vault = vault ?? new VaultV2 { Version = 2, Items = [] };
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
