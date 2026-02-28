using ItchyPassword.Client.Services.VaultConnectors;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ItchyPassword.Client.Services;

/// <summary>
/// Manages the current vault session: the loaded vault data, connector
/// preferences (reader / writers), and the <see cref="IsUnlocked"/> flag
/// that the UI uses for navigation guards and conditional rendering.
/// Provides symmetric <see cref="UnlockAsync"/> and <see cref="SaveVaultAsync"/>
/// methods so the vault lifecycle is managed in one place.
/// </summary>
public class VaultSession : INotifyPropertyChanged
{
    private const string ReaderStorageKey = "itchypassword_reader_vault_connector";
    private const string WriterIdsStorageKey = "itchypassword_writer_vault_connectors";

    private readonly IMasterKeyProvider _masterKeyProvider;
    private readonly VaultMigrationService _migrationService;
    private readonly LocalStorageService _storage;

    private bool _isInitialized;
    private Guid _readerId;
    private readonly HashSet<Guid> _writerIds = [];
    private VaultV2? _vault;

    /// <summary>
    /// Gets or sets the currently loaded vault.
    /// </summary>
    public VaultV2? Vault
    {
        get
        {
            return _vault;
        }
        set
        {
            if (_vault != value)
            {
                _vault = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUnlocked));
            }
        }
    }

    /// <summary>
    /// Gets the list of available vault connectors.
    /// </summary>
    public List<IVaultConnector> Connectors { get; } = [];

    /// <summary>
    /// Gets or sets the ID of the connector used for reading the vault.
    /// </summary>
    public Guid ReaderId
    {
        get
        {
            return _readerId;
        }
        set
        {
            if (_readerId != value)
            {
                _readerId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReadConnector));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultSession"/> class.
    /// </summary>
    /// <param name="masterKeyProvider">The provider for the in-memory master key.</param>
    /// <param name="connectors">The available vault connectors, resolved from the DI container.</param>
    /// <param name="storage">The local storage service used for persisting preferences.</param>
    /// <param name="migrationService">The service used to migrate legacy vault formats.</param>
    public VaultSession(IMasterKeyProvider masterKeyProvider, IEnumerable<IVaultConnector> connectors, LocalStorageService storage, VaultMigrationService migrationService)
    {
        _masterKeyProvider = masterKeyProvider;
        _storage = storage;
        _migrationService = migrationService;

        Connectors.AddRange(connectors);

        // Forward master-key changes so IsUnlocked re-evaluates for UI subscribers.
        if (_masterKeyProvider is INotifyPropertyChanged notifiable)
        {
            notifiable.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(IsUnlocked));
            };
        }

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
            // No saved preference (or stale ID) — persist the default so it's
            // available on the next launch without requiring a trip to Settings.
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

            OnPropertyChanged(nameof(WriteConnectors));
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
    /// <param name="id">The ID of the connector.</param>
    /// <returns>True if enabled for writing, otherwise false.</returns>
    public bool IsWriter(Guid id)
    {
        return _writerIds.Contains(id);
    }

    /// <summary>
    /// Enables or disables a connector for writing.
    /// </summary>
    /// <param name="id">The ID of the connector.</param>
    /// <param name="isEnabled">True to enable, false to disable.</param>
    public void SetWriter(Guid id, bool isEnabled)
    {
        if (isEnabled)
        {
            if (_writerIds.Add(id))
            {
                OnPropertyChanged(nameof(WriteConnectors));
            }
        }
        else
        {
            // Prevent removing the last writer.
            if (_writerIds.Count <= 1)
            {
                return;
            }

            if (_writerIds.Remove(id))
            {
                OnPropertyChanged(nameof(WriteConnectors));
            }
        }
    }

    /// <summary>
    /// Attempts to unlock the vault using the master key from the provider.
    /// Loads the vault from the active read connector, migrating legacy formats if necessary.
    /// </summary>
    /// <param name="onStatusChanged">Optional callback invoked with progress messages (e.g. during migration).</param>
    /// <returns>A tuple indicating success and an error message if failed.</returns>
    public async Task<(bool Success, string Error)> UnlockAsync(Action<string>? onStatusChanged = null)
    {
        if (_masterKeyProvider.HasMasterKey == false)
        {
            return (false, "Master key not provided.");
        }

        if (ReadConnector is null)
        {
            return (false, "No active vault connector selected.");
        }

        try
        {
            await ReadConnector.LoadConfigurationAsync();
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        if (ReadConnector.IsConfigured == false)
        {
            return (false, "Connector not configured.");
        }

        try
        {
            bool hasAccess = await ReadConnector.AccessAsync();

            if (hasAccess == false)
            {
                string errorMessage = ReadConnector.AccessFailureMessage
                    ?? $"Could not access {ReadConnector.Name}.";
                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        try
        {
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
                    }
                    else
                    {
                        return (false, "Unknown vault format or password incorrect.");
                    }
                }

                Vault = vault ?? new VaultV2 { Version = 2, Items = [] };
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Serializes the current vault and persists it to all enabled write connectors in parallel.
    /// </summary>
    /// <returns>
    /// An array of results, one per write connector, each indicating success or the error message.
    /// </returns>
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

    /// <summary>
    /// Gets a value indicating whether the vault is unlocked (loaded and decrypted).
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            return _masterKeyProvider.HasMasterKey && Vault is not null;
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="name">The name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
