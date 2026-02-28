using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ItchyPassword.Client.Services;

/// <summary>
/// Provides read and write access to the in-memory master key.
/// This service is deliberately separate from <see cref="VaultSession"/> so that
/// vault connectors can access the master key without creating a circular
/// dependency with the state that holds the connector list.
/// </summary>
public interface IMasterKeyProvider
{
    /// <summary>
    /// Gets or sets the master key used to decrypt the vault.
    /// </summary>
    string MasterKey { get; set; }

    /// <summary>
    /// Gets a value indicating whether a non-empty master key has been set.
    /// </summary>
    bool HasMasterKey { get; }
}

/// <summary>
/// Holds the in-memory master key for the current session.
/// Registered as a scoped (effectively singleton in Blazor WASM) service so that
/// both <see cref="VaultSession"/> and the vault connectors share the same instance
/// without a circular dependency.
/// </summary>
public class MasterKeyProvider : IMasterKeyProvider, INotifyPropertyChanged
{
    private string _masterKey = string.Empty;

    /// <inheritdoc />
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
                bool hadMasterKey = HasMasterKey;

                _masterKey = value;
                OnPropertyChanged();

                if (HasMasterKey != hadMasterKey)
                {
                    OnPropertyChanged(nameof(HasMasterKey));
                }
            }
        }
    }

    /// <inheritdoc />
    public bool HasMasterKey
    {
        get
        {
            return string.IsNullOrWhiteSpace(MasterKey) == false;
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="name">The name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
