using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ItchyPassword.Client.Services.VaultConnectors;

namespace ItchyPassword.Client.Services;

/// <summary>
/// Manages the state of the client vault.
/// </summary>
public class ClientVaultState : INotifyPropertyChanged
{
    private const string ActiveReaderStorageKey = "itchypassword_active_reader_vault_connector";
    private const string WriterIdsStorageKey = "itchypassword_writer_vault_connectors";

    private readonly LocalStorageService _storage;
    private bool _initialized;

    private Vault? _vault;

    /// <summary>
    /// Gets or sets the currently loaded vault.
    /// </summary>
    public Vault? Vault
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

    private string _masterKey = "";

    /// <summary>
    /// Gets or sets the master key used to decrypt the vault.
    /// </summary>
    public string MasterKey
    {
        get
        {
            return _masterKey;
        }
        set
        {
            if (_masterKey != value)
            {
                _masterKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMasterKey));
                OnPropertyChanged(nameof(IsUnlocked));
            }
        }
    }

    /// <summary>
    /// Gets the list of available vault connectors.
    /// </summary>
    public List<IVaultConnector> Connectors { get; } = [];

    private Guid _activeReaderId;

    /// <summary>
    /// Gets or sets the ID of the connector used for reading the vault.
    /// </summary>
    public Guid ActiveReaderId
    {
        get
        {
            return _activeReaderId;
        }
        set
        {
            if (_activeReaderId != value)
            {
                _activeReaderId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReadConnector));
            }
        }
    }

    private readonly HashSet<Guid> _writerIds = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientVaultState"/> class.
    /// </summary>
    /// <param name="http">The HTTP client used by connectors.</param>
    /// <param name="storage">The storage service used by connectors.</param>
    /// <param name="crypto">The crypto service used by connectors for encrypting/decrypting secrets.</param>
    public ClientVaultState(HttpClient http, LocalStorageService storage, ICryptoService crypto)
    {
        _storage = storage;

        var gh = new GitHubVaultConnector(http, storage, crypto, this);
        var gd = new GoogleDriveVaultConnector(http, storage, crypto, this);

        Connectors.Add(gh);
        Connectors.Add(gd);

        // Default to the first connector; InitializeAsync will override with the saved preference.
        ActiveReaderId = gh.Id;
        SetWriter(gh.Id, true);
        SetWriter(gd.Id, true);
    }

    /// <summary>
    /// Loads persisted preferences (e.g. active reader) from local storage.
    /// Safe to call multiple times; only the first call performs work.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        string? savedId = await _storage.GetItemAsync(ActiveReaderStorageKey);

        if (Guid.TryParse(savedId, out Guid id) && Connectors.Any(c => c.Id == id))
        {
            ActiveReaderId = id;
        }
        else
        {
            // No saved preference (or stale ID) — persist the default so it's
            // available on the next launch without requiring a trip to Settings.
            await SaveActiveReaderAsync();
        }

        string? savedWriters = await _storage.GetItemAsync(WriterIdsStorageKey);

        if (!string.IsNullOrWhiteSpace(savedWriters))
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
    /// Persists the active reader selection to local storage.
    /// </summary>
    public async Task SaveActiveReaderAsync()
    {
        await _storage.SetItemAsync(ActiveReaderStorageKey, ActiveReaderId.ToString());
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
            return Connectors.FirstOrDefault(c => c.Id == ActiveReaderId);
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
    /// <param name="enabled">True to enable, false to disable.</param>
    public void SetWriter(Guid id, bool enabled)
    {
        if (enabled)
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
    /// Gets a value indicating whether a master key has been set.
    /// </summary>
    public bool HasMasterKey
    {
        get
        {
            return string.IsNullOrWhiteSpace(MasterKey) == false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the vault is unlocked (loaded and decrypted).
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            return HasMasterKey && Vault is not null;
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
