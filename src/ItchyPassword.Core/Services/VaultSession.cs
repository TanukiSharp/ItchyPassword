using ItchyPassword.Core.Connectors;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Exceptions;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Manages the current vault session: the loaded vault data and connector configurations.
/// </summary>
public class VaultSession
{
    private const string ReaderStorageKey = "itchypassword_reader_vault_connector";
    private const string WriterIdsStorageKey = "itchypassword_writer_vault_connectors";

    private readonly IMasterKeyProvider _masterKeyProvider;
    private readonly VaultMigrationService _migrationService;
    private readonly ILocalStorageService _storage;

    private bool _isInitialized;
    private readonly HashSet<Guid> _writerIds = [];

    /// <summary>
    /// Gets or sets the currently loaded vault.
    /// </summary>
    public VaultV2? Vault { get; set; }

    /// <summary>
    /// Gets the list of available vault connectors.
    /// </summary>
    public List<IVaultConnector> Connectors { get; } = [];

    /// <summary>
    /// Gets or sets the ID of the connector used for reading the vault.
    /// </summary>
    public Guid ReaderId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultSession"/> class.
    /// </summary>
    public VaultSession(IMasterKeyProvider masterKeyProvider, IEnumerable<IVaultConnector> connectors, ILocalStorageService storage, VaultMigrationService migrationService)
    {
        _masterKeyProvider = masterKeyProvider;
        _storage = storage;
        _migrationService = migrationService;

        Connectors.AddRange(connectors);

        if (Connectors.Count > 0)
        {
            // Default to the first connector; InitializeAsync will override with the saved preference.
            ReaderId = Connectors[0].Id;
            SetWriter(Connectors[0].Id, true);
        }
    }

    /// <summary>
    /// Loads persisted preferences (e.g. active reader) from local storage.
    /// Safe to call multiple times; only the first call performs work.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        string? savedId = await _storage.GetItemAsync(ReaderStorageKey);

        if (Guid.TryParse(savedId, out Guid id) && Connectors.Any(c => c.Id == id))
        {
            ReaderId = id;
        }
        else
        {
            // Persistence default.
            await SaveReaderAsync();
        }

        string? savedWriters = await _storage.GetItemAsync(WriterIdsStorageKey);

        if (string.IsNullOrWhiteSpace(savedWriters) == false)
        {
            _writerIds.Clear();

            foreach (string part in savedWriters.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(part.Trim(), out Guid writerId) && Connectors.Any(c => c.Id == writerId))
                {
                    _writerIds.Add(writerId);
                }
            }
        }
        else
        {
            await SaveWritersAsync();
        }
    }

    /// <summary>
    /// Persists the reader selection to local storage.
    /// </summary>
    public async Task SaveReaderAsync()
    {
        await _storage.SetItemAsync(ReaderStorageKey, ReaderId.ToString());
    }

    /// <summary>
    /// Persists the writer connector selections to local storage.
    /// </summary>
    public async Task SaveWritersAsync()
    {
        string value = string.Join(",", _writerIds);
        await _storage.SetItemAsync(WriterIdsStorageKey, value);
    }

    /// <summary>
    /// Gets the connector currently used for reading.
    /// </summary>
    public IVaultConnector? ReadConnector
    {
        get
        {
            return Connectors.FirstOrDefault(c => c.Id == ReaderId);
        }
    }

    /// <summary>
    /// Gets the list of connectors enabled for writing.
    /// </summary>
    public IEnumerable<IVaultConnector> WriteConnectors
    {
        get
        {
            return Connectors.Where(c => IsWriter(c.Id));
        }
    }

    /// <summary>
    /// Checks if a connector is enabled for writing.
    /// </summary>
    public bool IsWriter(Guid id)
    {
        return _writerIds.Contains(id);
    }

    /// <summary>
    /// Enables or disables a connector for writing.
    /// </summary>
    public void SetWriter(Guid id, bool isEnabled)
    {
        if (isEnabled)
        {
            _writerIds.Add(id);
        }
        else
        {
            // Prevent removing the last writer.
            if (_writerIds.Count <= 1)
            {
                return;
            }

            _writerIds.Remove(id);
        }
    }

    /// <summary>
    /// Attempts to unlock the vault using the master key from the provider.
    /// Loads the vault from the active read connector, migrating legacy formats if necessary.
    /// </summary>
    public async Task UnlockAsync(Action<string>? onStatusChanged = null, Action? onVaultAccessGranted = null)
    {
        if (_masterKeyProvider.HasMasterKey == false)
        {
            throw new InvalidOperationException("Master key not provided.");
        }

        if (ReadConnector is null)
        {
            throw new InvalidOperationException("No active vault connector selected.");
        }

        await ReadConnector.LoadConfigurationAsync();

        if (ReadConnector.IsConfigured == false)
        {
            throw new VaultConnectorNotConfiguredException("Connector not configured.");
        }

        onStatusChanged?.Invoke("Accessing vault...");

        bool hasAccess = await ReadConnector.AccessAsync();

        if (hasAccess == false)
        {
            string errorMessage = ReadConnector.AccessFailureMessage
                ?? $"Could not access {ReadConnector.Name}.";
            throw new InvalidOperationException(errorMessage);
        }

        onVaultAccessGranted?.Invoke();

        onStatusChanged?.Invoke("Loading vault data...");

        string content = await ReadConnector.LoadVaultAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            Vault = new VaultV2() { Version = 2, Items = [] };
        }
        else
        {
            VaultV2? vault = VaultDataService.DeserializeVault(content);

            if (vault is null)
            {
                if (VaultMigrationService.IsLegacyVault(content))
                {
                    onStatusChanged?.Invoke("Migrating vault...");
                    var migrationProgress = new Progress<double>(percent => onStatusChanged?.Invoke($"Migrating vault... {percent:f1}%"));
                    vault = await _migrationService.MigrateAsync(content, _masterKeyProvider.MasterKey, migrationProgress);

                    // Successfully migrated. Save the new format immediately to avoid re-migration next time.
                    Vault = vault;
                    onStatusChanged?.Invoke("Saving migrated vault...");
                    await SaveVaultAsync();
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
    /// Serializes the current vault and persists it to all enabled write connectors in parallel.
    /// </summary>
    public async Task<(IVaultConnector Connector, bool Success, string Error)[]> SaveVaultAsync()
    {
        if (Vault is null)
        {
            return [];
        }

        string json = VaultDataService.SerializeVault(Vault);

        var tasks = WriteConnectors.Select(async c =>
        {
            try
            {
                await c.SaveVaultAsync(json);
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
