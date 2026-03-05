using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.AspNetCore.Components;

namespace ItchyPassword.Client.Services;

public class AppState(NavigationManager nav, VaultSession session, IMasterKeyProvider keyProvider) : IAppState
{
    private readonly NavigationManager _nav = nav;
    private readonly VaultSession _session = session;
    private readonly IMasterKeyProvider _keyProvider = keyProvider;
    private AppStatus _status = AppStatus.Locked;
    private string _statusMessage = string.Empty;
    private string _searchQuery = string.Empty;
    private Task? _currentUnlockTask;

    public event Action? OnChange;

    public AppStatus Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                NotifyStateChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                NotifyStateChanged();
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                NotifyStateChanged();
            }
        }
    }

    public async Task UnlockAsync(byte[] key, CancellationToken cancellationToken)
    {
        if (Status == AppStatus.Unlocking)
        {
            // Already unlocking, maybe attach to existing task if we tracked it,
            // or just return. For now, we'll start a new one or ignore if same key.
            // But usually this means user re-submitted form.
        }

        _keyProvider.MasterKey = key;
        await StartUnlockFlowAsync(cancellationToken);
    }

    public async Task RetryUnlockAsync(CancellationToken cancellationToken)
    {
        if (!_keyProvider.HasMasterKey)
        {
            Lock();
            return;
        }

        await StartUnlockFlowAsync(cancellationToken);
    }

    private async Task StartUnlockFlowAsync(CancellationToken cancellationToken)
    {
        Status = AppStatus.Unlocking;
        StatusMessage = "Accessing vault...";

        // Navigate immediately to vault view which will show spinner based on Unlocking status
        if (_nav.Uri.Contains("/vault") == false)
        {
            _nav.NavigateTo("/vault");
        }

        try
        {
            // We capture the task to ensure we can await it if needed,
            // but primarily to ensure the fire-and-forget nature doesn't swallow exceptions
            // before we handle them.
            _currentUnlockTask = _session.UnlockAsync(
                msg =>
                {
                    StatusMessage = msg;
                    NotifyStateChanged(); // Force update on message change
                },
                () =>
                {
                    Status = AppStatus.LoadingVault;
                    // Message will be updated by UnlockAsync's status callback immediately after this
                    NotifyStateChanged();
                },
                cancellationToken
            );

            await _currentUnlockTask;

            Status = AppStatus.Unlocked;
            StatusMessage = string.Empty;
        }
        catch (VaultConnectorNotConfiguredException)
        {
            Status = AppStatus.SetupRequired;
            StatusMessage = "Vault not configured.";
            _nav.NavigateTo("/settings");
        }
        catch (Exception ex)
        {
            Status = AppStatus.Error;
            StatusMessage = "Failed to unlock vault. " + ex.Message;
            if (ex is VaultDecryptionException)
            {
                 StatusMessage = "Failed to unlock vault. Master Key is likely incorrect.";
            }
            // Stay on current page (likely /vault) to show error
        }
        finally
        {
            _currentUnlockTask = null;
        }
    }

    public void Lock()
    {
        _keyProvider.MasterKey = [];
        _session.Vault = null;

        foreach (IVaultConnector connector in _session.Connectors)
        {
            foreach (ConfigurationEntry entry in connector.Configuration)
            {
                if (entry.IsEncrypted)
                {
                    entry.Value = string.Empty;
                }
            }

            connector.ClearSecrets();
        }

        Status = AppStatus.Locked;
        StatusMessage = string.Empty;
        SearchQuery = string.Empty;
        _nav.NavigateTo("/");
    }

    public async Task ReloadVaultAsync(CancellationToken cancellationToken)
    {
        if (Status == AppStatus.Unlocked && _keyProvider.HasMasterKey)
        {
            await StartUnlockFlowAsync(cancellationToken);
        }
    }

    public void Configure()
    {
        // User wants to go to settings, maybe from error screen
        _nav.NavigateTo("/settings");
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
