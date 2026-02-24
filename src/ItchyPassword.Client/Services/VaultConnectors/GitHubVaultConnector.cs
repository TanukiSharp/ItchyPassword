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
/// <param name="state">The client vault state providing the master key.</param>
public class GitHubVaultConnector(HttpClient http, LocalStorageService storage, ICryptoService crypto, ClientVaultState state) : IVaultConnector
{
    private const string RepositoryOwnerKey = "VaultRepositoryOwner";
    private const string RepositoryNameKey = "VaultRepositoryName";
    private const string FilePathKey = "VaultFilePath";
    private const string PersonalAccessTokenKey = "PersonalAccessToken";

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
            Key = RepositoryOwnerKey,
            Label = "Repository Owner",
            Description = "The GitHub username or organization that owns the repository.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "my-github-username",
            StorageKey = "itchypassword_github_repository_owner",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = RepositoryNameKey,
            Label = "Repository Name",
            Description = "The name of the GitHub repository that stores the vault file.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "my-vault-repo",
            StorageKey = "itchypassword_github_repository_name",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = FilePathKey,
            Label = "Vault File Path",
            Description = "Path to the vault file inside the repository.",
            Kind = ConfigurationEntryKind.Text,
            DefaultValue = "vault.json",
            Placeholder = "vault.json",
            StorageKey = "itchypassword_github_vault_file_path",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = PersonalAccessTokenKey,
            Label = "Personal Access Token",
            Description = "A GitHub PAT with read/write access to the repository contents.",
            Kind = ConfigurationEntryKind.Secret,
            Placeholder = "ghp_...",
            StorageKey = "itchypassword_github_personal_access_token",
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

    /// <inheritdoc />
    public async Task LoadConfigurationAsync()
    {
        string? masterKey = state.HasMasterKey ? state.MasterKey : null;
        await VaultConnectorHelper.LoadEntriesAsync(Configuration, storage, masterKey, crypto);
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync()
    {
        string? masterKey = state.HasMasterKey ? state.MasterKey : null;
        await VaultConnectorHelper.SaveEntriesAsync(Configuration, storage, masterKey, crypto);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync()
    {
        if (IsConfigured == false)
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenKey));
            using HttpResponseMessage response = await http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync()
    {
        if (IsConfigured == false)
        {
            throw new InvalidOperationException("Not configured.");
        }

        string owner = VaultConnectorHelper.GetValue(Configuration, RepositoryOwnerKey);
        string repository = VaultConnectorHelper.GetValue(Configuration, RepositoryNameKey);
        string path = VaultConnectorHelper.GetValue(Configuration, FilePathKey);
        string token = VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repository}/contents/{path}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        using HttpResponseMessage response = await http.SendAsync(request);

        if (response.IsSuccessStatusCode == false)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _currentSha = string.Empty;
                return string.Empty;
            }

            throw new Exception($"GitHub API Error: {response.StatusCode}");
        }

        GitHubFileResponse? json = await response.Content.ReadFromJsonAsync<GitHubFileResponse>();

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
    public async Task SaveVaultAsync(string content)
    {
        if (IsConfigured == false)
        {
            throw new InvalidOperationException("Not configured");
        }

        string owner = VaultConnectorHelper.GetValue(Configuration, RepositoryOwnerKey);
        string repository = VaultConnectorHelper.GetValue(Configuration, RepositoryNameKey);
        string path = VaultConnectorHelper.GetValue(Configuration, FilePathKey);
        string token = VaultConnectorHelper.GetValue(Configuration, PersonalAccessTokenKey);

        string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        var payload = new
        {
            message = "Update vault via ItchyPassword",
            content = base64Content,
            sha = _currentSha,
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.github.com/repos/{owner}/{repository}/contents/{path}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        GitHubUpdateResponse? json = await response.Content.ReadFromJsonAsync<GitHubUpdateResponse>();
        string newSha = json?.content?.sha ?? string.Empty;

        _currentSha = newSha;
    }

#pragma warning disable IDE1006 // Naming Styles
    private class GitHubFileResponse
    {
        public required string content { get; init; }
        public required string sha { get; init; }
    }

    private class GitHubUpdateResponse
    {
        public required GitHubFileResponse content { get; init; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
