using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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
    /// Gets or sets the master key as raw bytes.
    /// The caller (typically <see cref="Components.MasterKeyView"/>) converts the
    /// user-entered string to UTF-8 bytes once; every downstream consumer works
    /// with <c>byte[]</c> directly, avoiding repeated conversions.
    /// </summary>
    byte[] MasterKey { get; set; }

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
                CryptographicOperations.ZeroMemory(_masterKey);

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
        return a.AsSpan().SequenceEqual(b);
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
