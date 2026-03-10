using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Manages the current vault session: the loaded vault data and active connector selection.
/// </summary>
public class VaultSession
{
    private const string ActiveConnectorStorageKey = "itchypassword_main_vault_connector";

    private readonly IMasterKeyProvider _masterKeyProvider;
    private readonly ICryptoService _crypto;
    private readonly VaultMigrationService _migrationService;
    private readonly ILocalStorageService _storage;

    private bool _isInitialized;
    private Guid _activeConnectorId;

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
    /// Gets the currently active connector, or <c>null</c> if none is selected.
    /// </summary>
    public IVaultConnector? ActiveConnector
    {
        get
        {
            return Connectors.FirstOrDefault(c => c.Id == _activeConnectorId);
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
            _activeConnectorId = Connectors[0].Id;
        }
    }

    /// <summary>
    /// Loads the persisted active connector selection from local storage.
    /// Safe to call multiple times; only the first call performs work.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        string? savedId = await _storage.GetItemAsync(ActiveConnectorStorageKey, cancellationToken);

        if (Guid.TryParse(savedId, out Guid id) && Connectors.Any(c => c.Id == id))
        {
            _activeConnectorId = id;
        }
        else
        {
            // Persist the default.
            await SaveActiveConnectorAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Persists the active connector selection to local storage.
    /// </summary>
    public async Task SaveActiveConnectorAsync(CancellationToken cancellationToken)
    {
        await _storage.SetItemAsync(ActiveConnectorStorageKey, _activeConnectorId.ToString(), cancellationToken);
    }

    /// <summary>
    /// Returns whether the given connector is the active one.
    /// </summary>
    public bool IsActiveConnector(Guid id)
    {
        return id == _activeConnectorId;
    }

    /// <summary>
    /// Sets the active connector.
    /// </summary>
    public void SetActiveConnector(Guid id)
    {
        _activeConnectorId = id;
    }

    /// <summary>
    /// Attempts to load the vault using the master key from the provider.
    /// Loads the vault from the active connector, migrating legacy formats if necessary.
    /// </summary>
    public async Task LoadAsync(Action<string>? onStatusChanged, Action? onVaultAccessGranted, CancellationToken cancellationToken)
    {
        if (_masterKeyProvider.HasMasterKey == false)
        {
            throw new InvalidOperationException("Master key not provided.");
        }

        if (ActiveConnector is null)
        {
            throw new InvalidOperationException("No active vault connector selected.");
        }

        await ActiveConnector.LoadConfigurationAsync(cancellationToken);

        if (ActiveConnector.IsConfigured == false)
        {
            throw new VaultConnectorNotConfiguredException("Connector not configured.");
        }

        onStatusChanged?.Invoke("Accessing vault...");

        bool hasAccess = await ActiveConnector.AccessAsync(cancellationToken);

        if (hasAccess == false)
        {
            string errorMessage = ActiveConnector.AccessFailureMessage
                ?? $"Could not access {ActiveConnector.Name}.";
            throw new InvalidOperationException(errorMessage);
        }

        onVaultAccessGranted?.Invoke();

        onStatusChanged?.Invoke("Loading vault data...");

        string content = await ActiveConnector.LoadVaultAsync(cancellationToken);
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
                    (bool success, string error) = await SaveVaultAsync("Migration of vault from v1 to v2", cancellationToken);

                    if (success == false)
                    {
                        throw new InvalidOperationException($"Migration successful, but failed to save: {error}");
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
    /// Does not modify the active connector selection — only writes data.
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
    /// Serializes the current vault and persists it to the active connector.
    /// </summary>
    public async Task<(bool Success, string Error)> SaveVaultAsync(string changeHint, CancellationToken cancellationToken)
    {
        if (Vault is null || ActiveConnector is null)
        {
            return (false, "No vault loaded or no active connector.");
        }

        try
        {
            string json = await VaultDataService.SerializeAndSignAsync(Vault.Value, _masterKeyProvider.MasterKey, _crypto, cancellationToken);
            LastRawContent = json;
            await ActiveConnector.SaveVaultAsync(json, changeHint, cancellationToken);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
