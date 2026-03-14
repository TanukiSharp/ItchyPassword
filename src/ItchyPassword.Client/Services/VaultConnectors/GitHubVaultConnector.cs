using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Connector for interacting with a password vault stored in a GitHub repository.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GitHubVaultConnector"/> class.
/// </remarks>
/// <param name="http">The HTTP client instance.</param>
/// <param name="storage">The storage service instance.</param>
/// <param name="crypto">The crypto service instance for encrypting/decrypting secrets.</param>
/// <param name="masterKeyProvider">The provider for the in-memory master key.</param>
public class GitHubVaultConnector(HttpClient http, ILocalStorageService storage, ICryptoService crypto, IMasterKeyProvider masterKeyProvider) : IVaultConnector
{
    private const string RepositoryOwnerConfigKey = "VaultRepositoryOwner";
    private const string RepositoryNameConfigKey = "VaultRepositoryName";
    private const string FilePathConfigKey = "VaultFilePath";
    private const string PersonalAccessTokenConfigKey = "PersonalAccessToken";

    private string _currentSha = string.Empty; // Internal SHA tracking

    /// <inheritdoc />
    public string Name
    {
        get
        {
            return "GitHub";
        }
    }

    /// <inheritdoc />
    public string Description
    {
        get
        {
            return "Uses a Personal Access Token (PAT).";
        }
    }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.Parse("8820f1ba-6d60-449a-8a5e-6d6556e9c1f6");

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationEntry> Configuration { get; } =
    [
        new ConfigurationEntry
        {
            Key = RepositoryOwnerConfigKey,
            Label = "Repository owner",
            Description = "The GitHub username or organization that owns the repository.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "my-github-username",
            StorageKey = "github_repository_owner",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = RepositoryNameConfigKey,
            Label = "Repository name",
            Description = "The name of the GitHub repository that stores the vault file.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "my-vault-repo",
            StorageKey = "github_repository_name",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = FilePathConfigKey,
            Label = "Vault file path",
            Description = "Path to the vault file inside the repository.",
            Kind = ConfigurationEntryKind.Text,
            DefaultValue = "vault.json",
            Placeholder = "vault.json",
            StorageKey = "github_vault_file_path",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = PersonalAccessTokenConfigKey,
            Label = "Personal Access Token",
            Description = "A GitHub PAT with read/write access to the repository contents.",
            Kind = ConfigurationEntryKind.Secret,
            Placeholder = "ghp_...",
            StorageKey = "github_personal_access_token",
            IsRequired = true,
            IsEncrypted = true,
        },
    ];

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            return VaultConnectorHelper.AreRequiredEntriesFilled(Configuration);
        }
    }

    public Task ClearSecretsAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        byte[]? masterKey = masterKeyProvider.HasMasterKey ? masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.LoadEntriesAsync(Configuration, storage, masterKey, crypto, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        byte[]? masterKey = masterKeyProvider.HasMasterKey ? masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.SaveEntriesAsync(Configuration, storage, masterKey, crypto, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AccessAsync(CancellationToken cancellationToken)
    {
        if (IsConfigured == false)
        {
            return false;
        }

        try
        {
            string owner = VaultConnectorHelper.GetValue(Configuration, RepositoryOwnerConfigKey);
            string repository = VaultConnectorHelper.GetValue(Configuration, RepositoryNameConfigKey);
            string token = VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenConfigKey);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return false;
            }

            GitHubRepoResponse? repo = await response.Content.ReadFromJsonAsync<GitHubRepoResponse>(cancellationToken);

            if (repo?.permissions is null)
            {
                return false;
            }

            return repo.permissions.pull && repo.permissions.push;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync(CancellationToken cancellationToken)
    {
        if (IsConfigured == false)
        {
            throw new InvalidOperationException("Not configured.");
        }

        string owner = VaultConnectorHelper.GetValue(Configuration, RepositoryOwnerConfigKey);
        string repository = VaultConnectorHelper.GetValue(Configuration, RepositoryNameConfigKey);
        string path = VaultConnectorHelper.GetValue(Configuration, FilePathConfigKey);
        string token = VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenConfigKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/contents/{Uri.EscapeDataString(path)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode == false)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _currentSha = string.Empty;
                return string.Empty;
            }

            throw new Exception($"GitHub API Error: {response.StatusCode}");
        }

        GitHubFileResponse? json = await response.Content.ReadFromJsonAsync<GitHubFileResponse>(cancellationToken);

        if (json is null)
        {
            return string.Empty;
        }

        string base64 = json.content.Replace("\n", string.Empty);
        byte[] bytes = Convert.FromBase64String(base64);
        string content = Encoding.UTF8.GetString(bytes);

        _currentSha = json.sha;

        return content;
    }

    /// <inheritdoc />
    public async Task SaveVaultAsync(string content, string changeHint, CancellationToken cancellationToken)
    {
        if (IsConfigured == false)
        {
            throw new InvalidOperationException("Not configured");
        }

        string owner = VaultConnectorHelper.GetValue(Configuration, RepositoryOwnerConfigKey);
        string repository = VaultConnectorHelper.GetValue(Configuration, RepositoryNameConfigKey);
        string path = VaultConnectorHelper.GetValue(Configuration, FilePathConfigKey);
        string token = VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenConfigKey);

        string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        var payload = new
        {
            message = changeHint,
            content = base64Content,
            sha = _currentSha,
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/contents/{Uri.EscapeDataString(path)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        GitHubUpdateResponse? json = await response.Content.ReadFromJsonAsync<GitHubUpdateResponse>(cancellationToken);
        string newSha = json?.content?.sha ?? string.Empty;

        _currentSha = newSha;
    }

#pragma warning disable IDE1006 // Naming Styles
    private class GitHubRepoPermissions
    {
        public bool pull { get; init; }
        public bool push { get; init; }
    }

    private class GitHubRepoResponse
    {
        public GitHubRepoPermissions? permissions { get; init; }
    }

    private class GitHubFileResponse
    {
        public required string content { get; init; }
        public required string sha { get; init; }
    }

    private class GitHubUpdateContentEntry
    {
        public required string sha { get; init; }
    }

    private class GitHubUpdateResponse
    {
        public required GitHubUpdateContentEntry content { get; init; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
