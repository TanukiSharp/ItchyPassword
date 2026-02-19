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
    public static readonly ConfigStorageKey VaultRepositoryOwnerKey = new("VaultRepositoryOwner", "itchypassword_github_repository_owner");
    public static readonly ConfigStorageKey VaultRepositoryNameKey = new("VaultRepositoryName", "itchypassword_github_repository_name");
    public static readonly ConfigStorageKey VaultFilePathKey = new("VaultFilePath", "itchypassword_github_vault_file_path");
    public static readonly ConfigStorageKey PersonalAccessTokenKey = new("PersonalAccessToken", "itchypassword_github_personal_access_token");
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
    public Dictionary<string, string> Configuration { get; } = new Dictionary<string, string>
    {
        [VaultRepositoryOwnerKey.Config] = string.Empty,
        [VaultRepositoryNameKey.Config] = string.Empty,
        [VaultFilePathKey.Config] = "vault.json",
        [PersonalAccessTokenKey.Config] = string.Empty,
    };

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            bool hasOwner = string.IsNullOrWhiteSpace(Configuration[VaultRepositoryOwnerKey.Config]) == false;
            bool hasRepository = string.IsNullOrWhiteSpace(Configuration[VaultRepositoryNameKey.Config]) == false;
            bool hasFile = string.IsNullOrWhiteSpace(Configuration[VaultFilePathKey.Config]) == false;
            bool hasToken = string.IsNullOrWhiteSpace(Configuration[PersonalAccessTokenKey.Config]) == false;
            return hasOwner && hasRepository && hasFile && hasToken;
        }
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync()
    {
        await VaultConnectorHelper.BindStorageToMemoryAsync(VaultRepositoryOwnerKey, storage, Configuration);
        await VaultConnectorHelper.BindStorageToMemoryAsync(VaultRepositoryNameKey, storage, Configuration);
        await VaultConnectorHelper.BindStorageToMemoryAsync(VaultFilePathKey, storage, Configuration);
        await VaultConnectorHelper.BindStorageToMemoryAsync(PersonalAccessTokenKey, storage, Configuration, async value =>
        {
            try
            {
                return state.HasMasterKey
                    ? await VaultConnectorHelper.DecryptIfNeededAsync(value, state.MasterKey, crypto)
                    : value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt personal access token for GitHub access, the master key may be incorrect.", ex);
            }
        });
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync()
    {
        await VaultConnectorHelper.BindMemoryToStorageAsync(VaultRepositoryOwnerKey, Configuration, storage);
        await VaultConnectorHelper.BindMemoryToStorageAsync(VaultRepositoryNameKey, Configuration, storage);
        await VaultConnectorHelper.BindMemoryToStorageAsync(VaultFilePathKey, Configuration, storage);
        await VaultConnectorHelper.BindMemoryToStorageAsync(PersonalAccessTokenKey, Configuration, storage, async value =>
        {
            if (state.HasMasterKey)
            {
                return await VaultConnectorHelper.EncryptAsync(value, state.MasterKey, crypto);
            }
            return null;
        });
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Configuration[PersonalAccessTokenKey.Config]);
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

        string owner = Configuration[VaultRepositoryOwnerKey.Config];
        string repository = Configuration[VaultRepositoryNameKey.Config];
        string path = Configuration[VaultFilePathKey.Config];
        string token = Configuration[PersonalAccessTokenKey.Config];

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

        string owner = Configuration[VaultRepositoryOwnerKey.Config];
        string repository = Configuration[VaultRepositoryNameKey.Config];
        string path = Configuration[VaultFilePathKey.Config];
        string token = Configuration[PersonalAccessTokenKey.Config];

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
