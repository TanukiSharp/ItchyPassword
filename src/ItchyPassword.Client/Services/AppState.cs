using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.AspNetCore.Components;

namespace ItchyPassword.Client.Services;

public class AppState(NavigationManager nav, VaultSession session, IMasterKeyProvider keyProvider, ToastService toast) : IAppState
{
    private readonly NavigationManager _nav = nav;
    private readonly VaultSession _session = session;
    private readonly IMasterKeyProvider _keyProvider = keyProvider;
    private readonly ToastService _toast = toast;
    private AppStatus _status = AppStatus.NotLoaded;
    private string _statusMessage = string.Empty;
    private string _searchQuery = string.Empty;
    private Task? _currentLoadTask;

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

    public async Task LoadAsync(byte[] key, CancellationToken cancellationToken)
    {
        if (key.Length < Core.Constants.MasterKeyConstants.MinimumLength)
        {
            Status = AppStatus.Error;
            StatusMessage = $"Master key must be at least {Core.Constants.MasterKeyConstants.MinimumLength} characters.";
            NotifyStateChanged();
            return;
        }
        if (Status == AppStatus.Loading)
        {
            // Already loading, maybe attach to existing task if we tracked it,
            // or just return. For now, we'll start a new one or ignore if same key.
            // But usually this means user re-submitted form.
        }

        _keyProvider.MasterKey = key;
        await StartLoadFlowAsync("Loading vault...", cancellationToken);
    }

    public async Task RetryLoadAsync(CancellationToken cancellationToken)
    {
        if (!_keyProvider.HasMasterKey)
        {
            await UnloadAsync();
            return;
        }

        await StartLoadFlowAsync("Retrying to load vault...", cancellationToken);
    }

    private async Task StartLoadFlowAsync(string statusMessage, CancellationToken cancellationToken)
    {
        Status = AppStatus.Loading;
        StatusMessage = statusMessage;

        // Navigate immediately to vault view which will show spinner based on Loading status.
        if (_nav.Uri.Contains("/vault") == false)
        {
            _nav.NavigateTo("/vault");
        }

        try
        {
            // We capture the task to ensure we can await it if needed,
            // but primarily to ensure the fire-and-forget nature doesn't swallow exceptions
            // before we handle them.
            _currentLoadTask = _session.LoadAsync(
                msg =>
                {
                    StatusMessage = msg;
                    NotifyStateChanged(); // Force update on message change
                },
                () =>
                {
                    Status = AppStatus.LoadingVault;
                    // Message will be updated by LoadAsync's status callback immediately after this.
                    NotifyStateChanged();
                },
                cancellationToken
            );

            await _currentLoadTask;

            Status = AppStatus.Loaded;
            StatusMessage = string.Empty;

            if (_session.LastSignatureStatus != VaultSignatureStatus.Valid)
            {
                _toast.Show("⚠️ Vault integrity check failed. ⚠️\nThe vault may have been modified outside the app.", TimeSpan.MaxValue);
            }
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
            StatusMessage = "Failed to load vault. " + ex.Message;
            if (ex is VaultDecryptionException)
            {
                 StatusMessage = "Failed to load vault. Master Key is likely incorrect.";
            }
            // Stay on current page (likely /vault) to show error.
        }
        finally
        {
            _currentLoadTask = null;
        }
    }

    public async Task UnloadAsync()
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

            await connector.ClearSecretsAsync();
        }

        Status = AppStatus.NotLoaded;
        StatusMessage = string.Empty;
        SearchQuery = string.Empty;
        _nav.NavigateTo("/");
    }

    public async Task ReloadVaultAsync(CancellationToken cancellationToken)
    {
        if (Status == AppStatus.Loaded && _keyProvider.HasMasterKey)
        {
            await StartLoadFlowAsync("Reloading vault...", cancellationToken);
        }
    }

    public void Configure()
    {
        // User wants to go to settings, maybe from error screen
        _nav.NavigateTo("/settings");
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
