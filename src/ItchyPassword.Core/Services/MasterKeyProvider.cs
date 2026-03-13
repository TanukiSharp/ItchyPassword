using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Holds the in-memory master key for the current session.
/// Registered as a scoped (effectively singleton in Blazor WASM) service so that
/// both VaultSession and the vault connectors share the same instance
/// without a circular dependency.
/// </summary>
public class MasterKeyProvider : IMasterKeyProvider, INotifyPropertyChanged
{
    private byte[] _masterKey = [];

    /// <inheritdoc />
    public byte[] MasterKey
    {
        get
        {
            return _masterKey;
        }
        set
        {
            if (SequenceEquals(_masterKey, value) == false)
            {
                bool hadMasterKey = HasMasterKey;

                // Zero out the old key material before replacing it.
                Array.Clear(_masterKey, 0, _masterKey.Length);

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
            return _masterKey.Length > 0;
        }
    }

    /// <summary>
    /// Compares two byte arrays for value equality.
    /// </summary>
    private static bool SequenceEquals(byte[] a, byte[] b)
    {
        return CryptographicOperations.FixedTimeEquals(a, b);
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
