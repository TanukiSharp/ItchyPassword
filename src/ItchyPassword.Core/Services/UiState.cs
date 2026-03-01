using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Holds cross-page UI state that must survive Blazor navigation but is not
/// persisted to storage. Keep this class lean — add fields only for transient
/// presentation concerns that do not belong in domain services.
/// </summary>
public class UiState : INotifyPropertyChanged
{
    private string _unlockStatus = "Accessing vault...";
    private bool _isDecryptionError;

    /// <summary>
    /// Gets or sets the search query used to filter vault items.
    /// Preserved across page navigations so the user does not lose their filter.
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    /// Holds the currently running unlock/migration task, if any.
    /// Allows the UI to attach to the existing task if the user navigates away and back.
    /// </summary>
    public Task? UnlockTask { get; set; }

    /// <summary>
    /// Gets or sets the current status message of the unlock/migration process.
    /// </summary>
    public string UnlockStatus
    {
        get => _unlockStatus;
        set
        {
            if (_unlockStatus != value)
            {
                _unlockStatus = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the vault failed to decrypt/open.
    /// Used to hide navigation or adjust layout during error recovery.
    /// </summary>
    public bool IsDecryptionError
    {
        get => _isDecryptionError;
        set
        {
            if (_isDecryptionError != value)
            {
                _isDecryptionError = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
