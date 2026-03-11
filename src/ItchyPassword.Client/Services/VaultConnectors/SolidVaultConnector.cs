using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services.VaultConnectors;

public class SolidVaultConnector(
    ILocalStorageService storage,
    ICryptoService crypto,
    IMasterKeyProvider masterKeyProvider,
    IJSRuntime js
) : IVaultConnector
{
    private const string PodUrlConfigKey = "PodUrl";
    private const string FilePathConfigKey = "FilePath";

    private bool _isAuthenticated;
    private string? _accessFailureMessage;

    public Guid Id { get; } = Guid.Parse("f98bb7e2-1a43-4e4c-b171-8c46de6a5f97");

    public string Name => "Solid Pod";

    public string Description => "Store your vault on a decentralized Solid Pod.";

    public bool IsConfigured =>
        string.IsNullOrWhiteSpace(VaultConnectorHelper.GetValue(Configuration, PodUrlConfigKey)) is false &&
        string.IsNullOrWhiteSpace(VaultConnectorHelper.GetValue(Configuration, FilePathConfigKey)) is false;

    public bool CanRetryAccess => true;

    public string? AccessFailureMessage => _accessFailureMessage;

    public IReadOnlyList<ConfigurationEntry> Configuration { get; } =
    [
        new ConfigurationEntry
        {
            Key = PodUrlConfigKey,
            Label = "Identity Provider (OIDC Issuer)",
            Description = "The URL of your Identity Provider where you log in (e.g. https://login.inrupt.com or https://solidcommunity.net). This is NOT your Pod storage URL.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "https://login.inrupt.com",
            StorageKey = "itchypassword_solid_pod_url",
            IsRequired = true
        },
        new ConfigurationEntry
        {
            Key = FilePathConfigKey,
            Label = "File Path",
            Description = "The URL or path within your Pod where the vault will be saved (e.g. https://mypod.solidcommunity.net/private/vault.json).",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "https://mypod.solidcommunity.net/private/vault.json",
            StorageKey = "itchypassword_solid_file_path",
            IsRequired = true
        }
    ];

    public async Task<bool> AccessAsync(CancellationToken cancellationToken)
    {
        _accessFailureMessage = null;

        if (IsConfigured is false)
        {
            _accessFailureMessage = "Solid connector is not fully configured.";
            return false;
        }

        try
        {
            string podUrl = VaultConnectorHelper.GetValue(Configuration, PodUrlConfigKey);
            _isAuthenticated = await js.InvokeAsync<bool>("solidVault.login", cancellationToken, podUrl);

            if (_isAuthenticated is false)
            {
                _accessFailureMessage = "Authentication failed or was cancelled.";
            }

            return _isAuthenticated;
        }
        catch (Exception ex)
        {
            _accessFailureMessage = $"Authentication error: {ex.Message}";
            return false;
        }
    }

    public async Task<string> LoadVaultAsync(CancellationToken cancellationToken)
    {
        if (IsConfigured is false)
        {
            throw new InvalidOperationException("Solid connector is not configured.");
        }

        if (_isAuthenticated is false)
        {
            throw new InvalidOperationException("Not authenticated with Solid Pod.");
        }

        string filePath = VaultConnectorHelper.GetValue(Configuration, FilePathConfigKey);

        try
        {
            return await js.InvokeAsync<string>("solidVault.loadVault", cancellationToken, filePath);
        }
        catch (JSException)
        {
            // If the file doesn't exist, return empty to create a new vault.
            return string.Empty;
        }
    }

    public async Task SaveVaultAsync(string content, string changeHint, CancellationToken cancellationToken)
    {
        if (IsConfigured is false)
        {
            throw new InvalidOperationException("Solid connector is not configured.");
        }

        if (_isAuthenticated is false)
        {
            throw new InvalidOperationException("Not authenticated with Solid Pod.");
        }

        string filePath = VaultConnectorHelper.GetValue(Configuration, FilePathConfigKey);

        await js.InvokeVoidAsync("solidVault.saveVault", cancellationToken, filePath, content);
    }

    public async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        await VaultConnectorHelper.LoadEntriesAsync(
            Configuration,
            storage,
            masterKeyProvider.MasterKey,
            crypto,
            cancellationToken
        );
    }

    public async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        await VaultConnectorHelper.SaveEntriesAsync(
            Configuration,
            storage,
            masterKeyProvider.MasterKey,
            crypto,
            cancellationToken
        );
    }

    public async Task ClearSecretsAsync()
    {
        if (_isAuthenticated)
        {
            try
            {
               await js.InvokeVoidAsync("solidVault.logout");
            }
            catch
            {
               // Best effort
            }
        }

        _isAuthenticated = false;
        _accessFailureMessage = null;
    }
}
