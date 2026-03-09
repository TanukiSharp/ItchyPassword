using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Manages the current vault session: the loaded vault data and connector role assignments.
/// </summary>
public class VaultSession
{
    private const string MainConnectorStorageKey = "itchypassword_main_vault_connector";
    private const string BackupConnectorIdsStorageKey = "itchypassword_backup_vault_connectors";

    private readonly IMasterKeyProvider _masterKeyProvider;
    private readonly ICryptoService _crypto;
    private readonly VaultMigrationService _migrationService;
    private readonly ILocalStorageService _storage;

    private bool _isInitialized;
    private Guid _mainConnectorId;
    private readonly HashSet<Guid> _backupConnectorIds = [];

    /// <summary>
    /// Gets or sets the currently loaded vault.
    /// </summary>
    public VaultV2? Vault { get; set; }

    /// <summary>
    /// Gets the signature verification status from the last vault load.
    /// </summary>
    public VaultSignatureStatus LastSignatureStatus { get; private set; } = VaultSignatureStatus.Missing;

    /// <summary>
    /// Gets the last downloaded or saved vault content as a raw string.
    /// </summary>
    public string? LastRawContent { get; private set; }

    /// <summary>
    /// Gets the list of available vault connectors.
    /// </summary>
    public List<IVaultConnector> Connectors { get; } = [];

    /// <summary>
    /// Gets the connector assigned the Main role.
    /// </summary>
    public IVaultConnector? MainConnector
    {
        get
        {
            return Connectors.FirstOrDefault(c => c.Id == _mainConnectorId);
        }
    }

    /// <summary>
    /// Gets the connectors assigned the Backup role.
    /// </summary>
    public IEnumerable<IVaultConnector> BackupConnectors
    {
        get
        {
            return Connectors.Where(c => _backupConnectorIds.Contains(c.Id));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultSession"/> class.
    /// </summary>
    public VaultSession(IMasterKeyProvider masterKeyProvider, IEnumerable<IVaultConnector> connectors, ILocalStorageService storage, ICryptoService crypto, VaultMigrationService migrationService)
    {
        _masterKeyProvider = masterKeyProvider;
        _storage = storage;
        _crypto = crypto;
        _migrationService = migrationService;

        Connectors.AddRange(connectors);

        if (Connectors.Count > 0)
        {
            // Default to the first connector; InitializeAsync will override with the saved preference.
            SetRole(Connectors[0].Id, ConnectorRole.Main);
        }
    }

    /// <summary>
    /// Loads persisted role assignments from local storage.
    /// Safe to call multiple times; only the first call performs work.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        string? savedMainId = await _storage.GetItemAsync(MainConnectorStorageKey, cancellationToken);

        if (Guid.TryParse(savedMainId, out Guid id) && Connectors.Any(c => c.Id == id))
        {
            _mainConnectorId = id;
        }
        else
        {
            // Persist the default.
            await SaveRolesAsync(cancellationToken);
        }

        string? savedBackupIds = await _storage.GetItemAsync(BackupConnectorIdsStorageKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(savedBackupIds) == false)
        {
            _backupConnectorIds.Clear();

            foreach (string part in savedBackupIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(part.Trim(), out Guid backupId)
                    && backupId != _mainConnectorId
                    && Connectors.Any(c => c.Id == backupId))
                {
                    _backupConnectorIds.Add(backupId);
                }
            }
        }
        else
        {
            await SaveRolesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Persists the current role assignments (main connector and backup set) to local storage.
    /// </summary>
    public async Task SaveRolesAsync(CancellationToken cancellationToken)
    {
        await _storage.SetItemAsync(MainConnectorStorageKey, _mainConnectorId.ToString(), cancellationToken);
        string value = string.Join(",", _backupConnectorIds);
        await _storage.SetItemAsync(BackupConnectorIdsStorageKey, value, cancellationToken);
    }

    /// <summary>
    /// Returns the role assigned to a connector.
    /// </summary>
    public ConnectorRole GetRole(Guid id)
    {
        if (id == _mainConnectorId)
        {
            return ConnectorRole.Main;
        }

        if (_backupConnectorIds.Contains(id))
        {
            return ConnectorRole.Backup;
        }

        return ConnectorRole.Disabled;
    }

    /// <summary>
    /// Assigns a role to a connector. Setting Main auto-removes the connector from backups.
    /// Setting Disabled is silently ignored for the Main connector.
    /// </summary>
    public void SetRole(Guid id, ConnectorRole role)
    {
        switch (role)
        {
            case ConnectorRole.Main:
                _mainConnectorId = id;
                _backupConnectorIds.Remove(id);
                break;

            case ConnectorRole.Backup:
                _backupConnectorIds.Add(id);
                break;

            case ConnectorRole.Disabled:
                // Prevent disabling the main connector.
                if (id == _mainConnectorId)
                {
                    return;
                }

                _backupConnectorIds.Remove(id);
                break;
        }
    }

    /// <summary>
    /// Attempts to unlock the vault using the master key from the provider.
    /// Loads the vault from the Main connector, migrating legacy formats if necessary.
    /// </summary>
    public async Task UnlockAsync(Action<string>? onStatusChanged, Action? onVaultAccessGranted, CancellationToken cancellationToken)
    {
        if (_masterKeyProvider.HasMasterKey == false)
        {
            throw new InvalidOperationException("Master key not provided.");
        }

        if (MainConnector is null)
        {
            throw new InvalidOperationException("No active vault connector selected.");
        }

        await MainConnector.LoadConfigurationAsync(cancellationToken);

        if (MainConnector.IsConfigured == false)
        {
            throw new VaultConnectorNotConfiguredException("Connector not configured.");
        }

        onStatusChanged?.Invoke("Accessing vault...");

        ConnectorAccessResult accessResult = await MainConnector.AccessAsync(cancellationToken);

        if (accessResult.CanRead == false)
        {
            string errorMessage = MainConnector.AccessFailureMessage
                ?? $"Could not access {MainConnector.Name}.";
            throw new InvalidOperationException(errorMessage);
        }

        onVaultAccessGranted?.Invoke();

        onStatusChanged?.Invoke("Loading vault data...");

        string content = await MainConnector.LoadVaultAsync(cancellationToken);
        LastRawContent = content;

        if (string.IsNullOrWhiteSpace(content))
        {
            Vault = new VaultV2() { Version = 2, Items = [] };
            LastSignatureStatus = VaultSignatureStatus.Valid;
        }
        else
        {
            VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(
                content, _masterKeyProvider.MasterKey, _crypto, cancellationToken);

            VaultV2? vault = result.Vault;
            LastSignatureStatus = result.SignatureStatus;

            if (vault is null)
            {
                if (VaultMigrationService.IsLegacyVault(content))
                {
                    onStatusChanged?.Invoke("Migrating vault...");
                    var migrationProgress = new Progress<double>(percent => onStatusChanged?.Invoke($"Migrating vault... {percent:f1}%"));
                    vault = await _migrationService.MigrateAsync(content, _masterKeyProvider.MasterKey, migrationProgress, cancellationToken);

                    // Successfully migrated. Save the new format immediately to avoid re-migration next time.
                    Vault = vault;
                    onStatusChanged?.Invoke("Saving migrated vault...");
                    var results = await SaveVaultAsync("Migration of vault from v1 to v2", cancellationToken);

                    var failures = results.Where(r => r.Success == false).ToList();
                    if (failures.Count > 0)
                    {
                        throw new InvalidOperationException("Migration successful, but failed to save: " + string.Join(", ", failures.Select(f => $"{f.Connector.Name}: {f.Error}")));
                    }
                }
                else
                {
                    throw new VaultFormatException();
                }
            }

            Vault = vault ?? new VaultV2 { Version = 2, Items = [] };
        }
    }

    /// <summary>
    /// Serializes the current vault and pushes it to a single specified connector.
    /// Does not modify role assignments — only writes data.
    /// </summary>
    /// <param name="connector">The target connector to push to.</param>
    /// <param name="changeHint">A description of the change being made.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple indicating success and an error message on failure.</returns>
    public async Task<(bool Success, string Error)> PushVaultToAsync(IVaultConnector connector, string changeHint, CancellationToken cancellationToken)
    {
        if (Vault is null)
        {
            return (false, "No vault loaded.");
        }

        try
        {
            string json = await VaultDataService.SerializeAndSignAsync(Vault.Value, _masterKeyProvider.MasterKey, _crypto, cancellationToken);
            await connector.SaveVaultAsync(json, changeHint, cancellationToken);
            LastRawContent = json;
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Replaces the current vault with data from already-downloaded content.
    /// Deserializes, verifies signature, and handles empty/legacy content.
    /// </summary>
    /// <param name="content">Raw vault JSON from a connector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadVaultFromContentAsync(string content, CancellationToken cancellationToken)
    {
        if (_masterKeyProvider.HasMasterKey == false)
        {
            throw new InvalidOperationException("Master key not provided.");
        }

        LastRawContent = content;

        if (string.IsNullOrWhiteSpace(content))
        {
            Vault = new VaultV2() { Version = 2, Items = [] };
            LastSignatureStatus = VaultSignatureStatus.Valid;
            return;
        }

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(
            content, _masterKeyProvider.MasterKey, _crypto, cancellationToken);

        VaultV2? vault = result.Vault;
        LastSignatureStatus = result.SignatureStatus;

        if (vault is null)
        {
            if (VaultMigrationService.IsLegacyVault(content))
            {
                vault = await _migrationService.MigrateAsync(content, _masterKeyProvider.MasterKey, null, cancellationToken);
                Vault = vault;

                // Save the migrated vault to all active connectors.
                var results = await SaveVaultAsync("Migration of vault from v1 to v2", cancellationToken);
                var failures = results.Where(r => r.Success == false).ToList();

                if (failures.Count > 0)
                {
                    throw new InvalidOperationException("Migration successful, but failed to save: " + string.Join(", ", failures.Select(f => $"{f.Connector.Name}: {f.Error}")));
                }

                return;
            }

            throw new Exceptions.VaultFormatException();
        }

        Vault = vault ?? new VaultV2 { Version = 2, Items = [] };
    }

    /// <summary>
    /// Serializes the current vault and persists it to all active connectors in parallel.
    /// </summary>
    public async Task<(IVaultConnector Connector, bool Success, string Error)[]> SaveVaultAsync(string changeHint, CancellationToken cancellationToken)
    {
        if (Vault is null)
        {
            return [];
        }

        string json = await VaultDataService.SerializeAndSignAsync(Vault.Value, _masterKeyProvider.MasterKey, _crypto, cancellationToken);
        LastRawContent = json;

        var tasks = Connectors.Where(c => c.Id == _mainConnectorId || _backupConnectorIds.Contains(c.Id)).Select(async c =>
        {
            try
            {
                await c.SaveVaultAsync(json, changeHint, cancellationToken);
                return (Connector: c, Success: true, Error: string.Empty);
            }
            catch (Exception ex)
            {
                return (Connector: c, Success: false, Error: ex.Message);
            }
        });

        return await Task.WhenAll(tasks);
    }
}
