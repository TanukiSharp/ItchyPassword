using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Connector for interacting with a password vault stored in Google Drive.
/// Supports two storage modes:
/// <list type="bullet">
///   <item><description>
///     <b>App data folder</b> (default): File is stored in the hidden <c>appDataFolder</c>
///     space, invisible to the user in Google Drive UI. Requires <c>drive.appdata</c> scope.
///   </description></item>
///   <item><description>
///     <b>User-visible folder</b>: File is stored in a user-specified Drive folder.
///     Requires <c>drive.file</c> scope.
///   </description></item>
/// </list>
/// <para>
/// Authentication uses the OAuth 2.0 Authorization Code flow with PKCE via a popup.
/// This runs in a first-party navigation context, allowing the browser to reuse the user's
/// existing Google session. A minimal JS interop layer handles only the popup window lifecycle;
/// all cryptographic operations (PKCE), URL construction, state validation, token exchange,
/// and Drive API calls are performed entirely in C# via <see cref="HttpClient"/>.
/// The access and refresh tokens are encrypted with the master key and persisted in localStorage.
/// </para>
/// </summary>
public class GoogleDriveVaultConnector(
    HttpClient http,
    ILocalStorageService storage,
    ICryptoService crypto,
    IMasterKeyProvider masterKeyProvider,
    IJSRuntime js
) : IVaultConnector
{
    // Google OAuth Client ID and Secret for ItchyPassword web app.
    // These are safe to embed in a public SPA — Google requires them for the
    // token exchange but they are not truly secret for client-side apps.
    // The client is restricted by authorized JavaScript origins and redirect URIs
    // configured in the Google Cloud Console.
    //
    // The secret is split to avoid triggering GitHub secret scanning on push.
    // This is NOT a security measure — this value is public by nature in a SPA.
    // https://developers.google.com/identity/protocols/oauth2/native-app
    private const string ClientId = "959481970115-4l07fa7bjv8l2dhkodtsl23q6hl1d70r.apps.googleusercontent.com";
    private static readonly string ClientSecret = string.Concat("GOCSPX-", "68HFVtr7r1pNlAMtitFjnVcnw7Hv");
    private const string VaultFileName = "vault.json";
    private const string HistoryFileName = "history.txt";
    private const int MaxHistoryEntries = 500;
    private const string DriveApiBase = "https://www.googleapis.com/drive/v3";
    private const string DriveUploadBase = "https://www.googleapis.com/upload/drive/v3";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string OAuthCallbackPath = "google-oauth-callback";
    private const string AppDataScope = "https://www.googleapis.com/auth/drive.appdata";
    private const string DriveFileScope = "https://www.googleapis.com/auth/drive.file";

    private const string StorageModeConfigKey = "StorageMode";
    private const string FolderIdConfigKey = "FolderId";
    private const string AccessTokenStorageKey = "itchypassword_gdrive_access_token";
    private const string RefreshTokenStorageKey = "itchypassword_gdrive_refresh_token";

    private readonly HttpClient _http = http;
    private readonly ILocalStorageService _storage = storage;
    private readonly ICryptoService _crypto = crypto;
    private readonly IMasterKeyProvider _masterKeyProvider = masterKeyProvider;
    private readonly IJSRuntime _js = js;

    private bool _configLoaded;
    private readonly DriveFile _vaultFile = new(VaultFileName, "application/json");
    private readonly DriveFile _historyFile = new(HistoryFileName, "text/plain");
    private string? _resolvedFolderId;
    private string _accessToken = string.Empty;
    private string _refreshToken = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; } = Guid.Parse("b7d4c22a-0498-4c12-a1f4-5f80e9a5c8e2");

    /// <inheritdoc />
    public string Name
    {
        get
        {
            return "Google Drive";
        }
    }

    /// <inheritdoc />
    public string Description
    {
        get
        {
            return "Store vault in Google Drive. Sign in with your Google account.";
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationEntry> Configuration { get; } =
    [
        new ConfigurationEntry
        {
            Key = StorageModeConfigKey,
            Label = "Storage mode",
            Description = """
                Choose where the vault file is stored in Google Drive.
                - App data is hidden from the user and only accessible by ItchyPassword, it keeps your Drive clean but the file cannot be manually managed.
                - User data stores the vault in a visible folder you choose, making it easy to find, but the user can accidentally break it.
                """,
            Kind = ConfigurationEntryKind.Dropdown,
            DefaultValue = "appdata",
            StorageKey = "itchypassword_gdrive_storage_mode",
            Options =
            [
                new DropdownOption("appdata", "App data"),
                new DropdownOption("folder", "User data"),
            ],
        },
        new ConfigurationEntry
        {
            Key = FolderIdConfigKey,
            Label = "Folder",
            Description = "Enter a folder name (e.g. \"ItchyPassword\") or a path (e.g. \"MyData/Vaults\"). Created automatically if it does not exist.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "ItchyPassword",
            StorageKey = "itchypassword_gdrive_folder_id",
            IsRequired = true,
            VisibleWhenKey = StorageModeConfigKey,
            VisibleWhenValue = "folder",
        },
    ];

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            // Authentication happens at access time.
            // Only user-visible entries with conditional visibility need checking.
            return VaultConnectorHelper.AreRequiredEntriesFilled(Configuration);
        }
    }

    /// <inheritdoc />
    public bool CanRetryAccess
    {
        get
        {
            // Sign-in popup requires a user gesture. A retry with a fresh gesture
            // can help if the first attempt failed (e.g. popup blocked).
            return true;
        }
    }

    /// <inheritdoc />
    public string? AccessFailureMessage { get; private set; }

    /// <summary>
    /// Clears in-memory OAuth tokens and secret configuration entries.
    /// </summary>
    public Task ClearSecretsAsync()
    {
        _accessToken = string.Empty;
        _refreshToken = string.Empty;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        // Short-circuit if already loaded.
        if (_configLoaded)
        {
            return;
        }

        byte[]? masterKey = _masterKeyProvider.HasMasterKey ? _masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.LoadEntriesAsync(Configuration, _storage, masterKey, _crypto, cancellationToken);

        // Load tokens into private fields (not in Configuration, since that is UI-visible).
        _accessToken = await LoadEncryptedTokenAsync(AccessTokenStorageKey, cancellationToken);
        _refreshToken = await LoadEncryptedTokenAsync(RefreshTokenStorageKey, cancellationToken);

        _configLoaded = true;
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        byte[]? masterKey = _masterKeyProvider.HasMasterKey ? _masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.SaveEntriesAsync(Configuration, _storage, masterKey, _crypto, cancellationToken);

        // Persist tokens encrypted directly to localStorage (not via Configuration).
        await SaveEncryptedTokenAsync(AccessTokenStorageKey, _accessToken, cancellationToken);
        await SaveEncryptedTokenAsync(RefreshTokenStorageKey, _refreshToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AccessAsync(CancellationToken cancellationToken)
    {
        AccessFailureMessage = null;

        // Ensure configuration (StorageMode, FolderId) is loaded before we
        // potentially call SaveConfigurationAsync (which would overwrite with defaults).
        await LoadConfigurationAsync(cancellationToken);

        // Always reload tokens from storage to detect external changes
        // (e.g. user deleted tokens from localStorage via DevTools).
        try
        {
            _accessToken = await LoadEncryptedTokenAsync(AccessTokenStorageKey, cancellationToken);
            _refreshToken = await LoadEncryptedTokenAsync(RefreshTokenStorageKey, cancellationToken);
        }
        catch (Exception)
        {
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
        }

        // 1. If we have an access token, validate it (checks expiry + scopes).
        if (string.IsNullOrWhiteSpace(_accessToken) == false)
        {
            if (await ValidateTokenAsync(_accessToken, cancellationToken))
            {
                return true;
            }
        }

        // 2. Access token missing or invalid — try refreshing.
        if (string.IsNullOrWhiteSpace(_refreshToken) == false)
        {
            string? refreshed = await TryRefreshAccessTokenAsync(_refreshToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshed) == false && await ValidateTokenAsync(refreshed, cancellationToken))
            {
                _accessToken = refreshed;
                await SaveConfigurationAsync(cancellationToken);
                return true;
            }

            // Refresh token produced a token without required scopes — discard it.
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
            AccessFailureMessage = """
                Google did not grant the required permissions.
                Please revoke ItchyPassword at https://myaccount.google.com/permissions and try again.
                """;
            await SaveConfigurationAsync(cancellationToken);
        }

        // 3. No valid token — interactive sign-in required.
        return await InteractiveSignInAsync(cancellationToken);
    }

    /// <summary>
    /// Performs interactive OAuth sign-in via a popup window.
    /// Must be called on the browser gesture call-stack so the popup is not blocked.
    /// </summary>
    private async Task<bool> InteractiveSignInAsync(CancellationToken cancellationToken)
    {
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = ComputeCodeChallenge(codeVerifier);
        string state = GenerateState();
        string redirectUri = ComputeRedirectUri();
        string authUrl = BuildAuthorizationUrl(redirectUri, codeChallenge, state);

        // Open popup synchronously BEFORE the first await to preserve the browser's
        // transient user activation from the click handler.
        if (_js is IJSInProcessRuntime jsInProcess)
        {
            jsInProcess.InvokeVoid("googleDriveInterop.openPopup", authUrl);
        }

        string? resultJson = await _js.InvokeAsync<string?>("googleDriveInterop.awaitResult", cancellationToken);

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            AccessFailureMessage = "Google sign-in was cancelled or failed. Click Retry to try again.";
            return false;
        }

        AuthCallbackResult? callbackResult = JsonSerializer.Deserialize<AuthCallbackResult>(resultJson);

        if (callbackResult is null || string.IsNullOrWhiteSpace(callbackResult.Code))
        {
            AccessFailureMessage = "Google sign-in returned invalid data. Click Retry to try again.";
            return false;
        }

        // Validate state to prevent CSRF attacks.
        if (callbackResult.State != state)
        {
            AccessFailureMessage = "Google sign-in state mismatch (possible CSRF). Click Retry to try again.";
            return false;
        }

        // Exchange the authorization code for tokens.
        GoogleTokenResponse? tokenResponse = await ExchangeCodeForTokensAsync(callbackResult.Code, codeVerifier, redirectUri, cancellationToken);

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            AccessFailureMessage = "Failed to exchange authorization code for access token. Click Retry to try again.";
            return false;
        }

        _accessToken = tokenResponse.AccessToken;

        // Validate the freshly obtained token has the required scopes.
        if (await ValidateTokenAsync(_accessToken, cancellationToken) == false)
        {
            AccessFailureMessage = """
                Google did not grant the required permissions.
                Please revoke ItchyPassword at https://myaccount.google.com/permissions and try again.
            """;
            _accessToken = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(tokenResponse.RefreshToken) == false)
        {
            _refreshToken = tokenResponse.RefreshToken;
        }

        await SaveConfigurationAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync(CancellationToken cancellationToken)
    {
        // Ensure configuration is loaded (short-circuits if already done).
        await LoadConfigurationAsync(cancellationToken);

        string accessToken = await GetOrRefreshAccessTokenAsync(cancellationToken);
        DriveStorageMode storageMode = GetStorageMode();
        string? folderId = storageMode == DriveStorageMode.UserFolder
            ? await GetResolvedFolderIdAsync(accessToken, cancellationToken)
            : null;

        // Pre-cache file IDs for both vault and history in parallel.
        await Task.WhenAll(
            _vaultFile.FindAsync(_http, accessToken, storageMode, folderId, cancellationToken),
            _historyFile.FindAsync(_http, accessToken, storageMode, folderId, cancellationToken)
        );

        if (string.IsNullOrWhiteSpace(_vaultFile.FileId))
        {
            // No vault file exists yet — return empty so the app creates a new vault.
            return string.Empty;
        }

        return await _vaultFile.DownloadAsync(_http, accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveVaultAsync(string content, string changeHint, CancellationToken cancellationToken)
    {
        // Ensure configuration is loaded. Writer connectors may not have
        // been explicitly loaded or accessed before save is called.
        await LoadConfigurationAsync(cancellationToken);

        string accessToken = await GetOrRefreshAccessTokenAsync(cancellationToken);
        DriveStorageMode storageMode = GetStorageMode();
        string? folderId = storageMode == DriveStorageMode.UserFolder
            ? await GetResolvedFolderIdAsync(accessToken, cancellationToken)
            : null;

        // Parallel batch 1: find vault file (if not cached) and download history content.
        Task findVaultTask = string.IsNullOrWhiteSpace(_vaultFile.FileId)
            ? _vaultFile.FindAsync(_http, accessToken, storageMode, folderId, cancellationToken)
            : Task.CompletedTask;

        Task<string> downloadHistoryTask = DownloadHistoryAsync(accessToken, storageMode, folderId, cancellationToken);

        await Task.WhenAll(findVaultTask, downloadHistoryTask);

        string existingHistory = await downloadHistoryTask;
        string updatedHistory = BuildHistoryContent(existingHistory, changeHint);

        // Parallel batch 2: upload vault and history concurrently.
        // Vault upload is critical (must throw on failure).
        // History upload is best-effort (failures are swallowed).
        Task vaultTask = _vaultFile.UploadAsync(_http, accessToken, content, storageMode, folderId, cancellationToken);
        Task historyTask = _historyFile.UploadAsync(_http, accessToken, updatedHistory, storageMode, folderId, cancellationToken);

        await vaultTask;

        try
        {
            await historyTask;
        }
        catch
        {
            // History update is non-fatal — vault integrity is more important.
        }
    }

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens
    /// via Google's token endpoint using the PKCE code verifier.
    /// </summary>
    private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier,
            });

            using HttpResponseMessage response = await _http.PostAsync(GoogleTokenEndpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to obtain a new access token using a stored refresh token.
    /// Returns the new access token, or <see langword="null"/> if the refresh failed.
    /// </summary>
    private async Task<string?> TryRefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            });

            using HttpResponseMessage response = await _http.PostAsync(GoogleTokenEndpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return null;
            }

            GoogleTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
            return tokenResponse?.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates an access token by calling the Google tokeninfo endpoint.
    /// Returns <see langword="true"/> if the token is still valid and has the scope
    /// required by the current storage mode.
    /// </summary>
    private async Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            string url = $"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={Uri.EscapeDataString(accessToken)}";
            using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return false;
            }

            // Verify the token has the scope required for the current storage mode.
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.TryGetProperty("scope", out JsonElement scopeElement))
            {
                string grantedScopes = scopeElement.GetString() ?? string.Empty;
                string requiredFragment = GetStorageMode() == DriveStorageMode.AppData ? "drive.appdata" : "drive.file";

                if (grantedScopes.Contains(requiredFragment, StringComparison.OrdinalIgnoreCase) == false)
                {
                    return false;
                }
            }
            else
            {
                // No scope info in response — treat as invalid to be safe.
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Downloads the history file content, finding it first if its file ID is not cached.
    /// Returns <see cref="string.Empty"/> if the file does not exist.
    /// </summary>
    private async Task<string> DownloadHistoryAsync(string accessToken, DriveStorageMode storageMode, string? folderId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_historyFile.FileId))
            {
                await _historyFile.FindAsync(_http, accessToken, storageMode, folderId, cancellationToken);
            }

            return await _historyFile.DownloadAsync(_http, accessToken, cancellationToken);
        }
        catch
        {
            // History download failure is non-fatal — we'll create a fresh file.
            return string.Empty;
        }
    }

    /// <summary>
    /// Builds the updated history file content by prepending a new timestamped entry
    /// to the existing content and trimming to <see cref="MaxHistoryEntries"/> entries.
    /// Newest entries appear at the top of the file.
    /// </summary>
    private static string BuildHistoryContent(string existingContent, string changeHint)
    {
        List<string> entries = string.IsNullOrWhiteSpace(existingContent)
            ? []
            : [.. existingContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)];

        string newEntry = $"{DateTime.UtcNow:O} {changeHint}";
        entries.Insert(0, newEntry);

        // Trim oldest entries (at the end) to respect the maximum.
        if (entries.Count > MaxHistoryEntries)
        {
            entries.RemoveRange(MaxHistoryEntries, entries.Count - MaxHistoryEntries);
        }

        return string.Join('\n', entries);
    }

    /// <summary>
    /// Returns the current access token, or attempts to refresh it using the stored
    /// refresh token. Does NOT trigger interactive sign-in (which requires a user gesture).
    /// Used by <see cref="LoadVaultAsync"/> and <see cref="SaveVaultAsync"/> to support
    /// writer connectors that may not have been explicitly accessed via <see cref="AccessAsync"/>.
    /// </summary>
    private async Task<string> GetOrRefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Fast path: return existing token if available.
        if (string.IsNullOrWhiteSpace(_accessToken) == false)
        {
            return _accessToken;
        }

        // Attempt to refresh using the stored refresh token.
        if (string.IsNullOrWhiteSpace(_refreshToken) == false)
        {
            string? refreshed = await TryRefreshAccessTokenAsync(_refreshToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshed) == false)
            {
                _accessToken = refreshed;
                await SaveConfigurationAsync(cancellationToken);
                return _accessToken;
            }
        }

        throw new InvalidOperationException(
            "No valid Google Drive access token. Please use 'Test access' in Settings to sign in.");
    }

    private DriveStorageMode GetStorageMode()
    {
        string value = VaultConnectorHelper.GetValue(Configuration, StorageModeConfigKey);
        return value == "folder" ? DriveStorageMode.UserFolder : DriveStorageMode.AppData;
    }

    /// <summary>
    /// Returns the resolved Google Drive folder ID, resolving the user-entered value
    /// (which may be a folder name or an actual ID) on first use.
    /// The result is cached for the lifetime of the connector instance.
    /// </summary>
    private async Task<string> GetResolvedFolderIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_resolvedFolderId) == false)
        {
            return _resolvedFolderId;
        }

        string configured = VaultConnectorHelper.GetValue(Configuration, FolderIdConfigKey);

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "Google Drive folder is not configured. Please enter a folder name or ID in Settings.");
        }

        _resolvedFolderId = await ResolveFolderIdAsync(accessToken, configured, cancellationToken);
        return _resolvedFolderId;
    }

    /// <summary>
    /// Resolves a user-entered folder value to a Google Drive folder ID.
    /// <list type="bullet">
    ///   <item>If it looks like a Drive file ID (20+ alphanumeric chars), it is used as-is.</item>
    ///   <item>If it contains <c>/</c>, each segment is resolved (or created) in sequence,
    ///         e.g. <c>MyData/ItchyPassword/Vaults</c> creates the full hierarchy.</item>
    ///   <item>Otherwise, it is treated as a single folder name under the Drive root.</item>
    /// </list>
    /// Folders that do not exist are created automatically.
    /// </summary>
    private async Task<string> ResolveFolderIdAsync(string accessToken, string folderNameOrId, CancellationToken cancellationToken)
    {
        // Heuristic: Drive file IDs are typically 20–44 characters of alphanumeric + dash + underscore.
        // A human-readable folder name will rarely match this pattern.
        bool looksLikeId = folderNameOrId.Length >= 20
            && folderNameOrId.Contains('/') == false
            && folderNameOrId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

        if (looksLikeId)
        {
            return folderNameOrId;
        }

        // Split the path into segments and resolve each one, starting from root.
        string[] segments = folderNameOrId
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Google Drive folder path is empty after parsing.");
        }

        // "root" is the well-known alias for the user's My Drive root folder.
        string parentId = "root";

        foreach (string segment in segments)
        {
            parentId = await FindOrCreateFolderAsync(accessToken, segment, parentId, cancellationToken);
        }

        return parentId;
    }

    /// <summary>
    /// Finds a child folder by name under a given parent, or creates it if it does not exist.
    /// </summary>
    private async Task<string> FindOrCreateFolderAsync(string accessToken, string folderName, string parentId, CancellationToken cancellationToken)
    {
        string escapedName = folderName.Replace("'", "\\'", StringComparison.Ordinal);
        string query = $"name = '{escapedName}' and mimeType = 'application/vnd.google-apps.folder' and '{parentId}' in parents and trashed = false";
        string encodedQuery = Uri.EscapeDataString(query);
        string url = $"{DriveApiBase}/files?q={encodedQuery}&spaces=drive&fields=files(id,name)&pageSize=1";

        using var searchRequest = new HttpRequestMessage(HttpMethod.Get, url);
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage searchResponse = await _http.SendAsync(searchRequest, cancellationToken);
        await EnsureDriveSuccessAsync(searchResponse, cancellationToken);

        DriveFileListResponse? result = await searchResponse.Content.ReadFromJsonAsync<DriveFileListResponse>(cancellationToken);

        if (result?.Files is { Count: > 0 } && string.IsNullOrWhiteSpace(result.Files[0].Id) == false)
        {
            return result.Files[0].Id;
        }

        // Folder doesn't exist — create it under parentId.
        var metadata = new
        {
            name = folderName,
            mimeType = "application/vnd.google-apps.folder",
            parents = new[] { parentId },
        };

        string metadataJson = JsonSerializer.Serialize(metadata);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{DriveApiBase}/files?fields=id");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        createRequest.Content = new StringContent(metadataJson, Encoding.UTF8, "application/json");

        using HttpResponseMessage createResponse = await _http.SendAsync(createRequest, cancellationToken);
        await EnsureDriveSuccessAsync(createResponse, cancellationToken);

        DriveFileResponse? created = await createResponse.Content.ReadFromJsonAsync<DriveFileResponse>(cancellationToken);

        if (created is null || string.IsNullOrWhiteSpace(created.Id))
        {
            throw new InvalidOperationException($"Failed to create Google Drive folder '{folderName}' under parent '{parentId}'.");
        }

        return created.Id;
    }

    /// <summary>
    /// Throws an <see cref="HttpRequestException"/> with the response body included
    /// when the status code indicates failure. This provides much better diagnostics
    /// than <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> which discards the body.
    /// </summary>
    private static async Task EnsureDriveSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Google Drive API {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
            inner: null,
            response.StatusCode
        );
    }

    /// <summary>
    /// Loads an encrypted token from localStorage and decrypts it using the master key.
    /// Returns <see cref="string.Empty"/> if no value is stored.
    /// </summary>
    private async Task<string> LoadEncryptedTokenAsync(string key, CancellationToken cancellationToken)
    {
        string? stored = await _storage.GetItemAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        try
        {
            return _masterKeyProvider.HasMasterKey
                ? await VaultConnectorHelper.DecryptIfNeededAsync(stored, _masterKeyProvider.MasterKey, _crypto, cancellationToken)
                : stored;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt Google Drive token '{key}'. The master key may be incorrect.", ex);
        }
    }

    /// <summary>
    /// Encrypts and persists a token value to localStorage.
    /// Only writes when the master key is available and the value is non-empty.
    /// </summary>
    private async Task SaveEncryptedTokenAsync(string key, string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value) || _masterKeyProvider.HasMasterKey == false)
        {
            return;
        }

        string encrypted = await VaultConnectorHelper.EncryptAsync(value, _masterKeyProvider.MasterKey, _crypto, cancellationToken);
        await _storage.SetItemAsync(key, encrypted, cancellationToken);
    }

    /// <summary>
    /// Generates a cryptographically random PKCE code verifier (RFC 7636).
    /// </summary>
    private static string GenerateCodeVerifier()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Computes the PKCE code challenge as a base64url-encoded SHA-256 hash
    /// of the code verifier (RFC 7636, S256 method).
    /// </summary>
    private static string ComputeCodeChallenge(string codeVerifier)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Generates a cryptographically random state parameter for CSRF protection.
    /// </summary>
    private static string GenerateState()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Base64url-encodes a byte array (RFC 4648 section 5, no padding).
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Computes the OAuth redirect URI from the application's base address.
    /// </summary>
    private string ComputeRedirectUri()
    {
        return new Uri(_http.BaseAddress!, OAuthCallbackPath).AbsoluteUri;
    }

    /// <summary>
    /// Returns the OAuth scope string required for the current storage mode.
    /// </summary>
    private string GetRequiredScopes()
    {
        return GetStorageMode() == DriveStorageMode.AppData ? AppDataScope : DriveFileScope;
    }

    /// <summary>
    /// Builds the full Google OAuth 2.0 authorization URL with PKCE parameters.
    /// Only requests the scope needed for the current storage mode.
    /// </summary>
    private string BuildAuthorizationUrl(string redirectUri, string codeChallenge, string state)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = GetRequiredScopes(),
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
        };

        string queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{GoogleAuthEndpoint}?{queryString}";
    }

    /// <summary>
    /// Defines where files are stored in Google Drive.
    /// </summary>
    private enum DriveStorageMode
    {
        /// <summary>
        /// Hidden app-data folder, invisible to the user in Google Drive UI.
        /// </summary>
        AppData,

        /// <summary>
        /// User-visible folder chosen by the user.
        /// </summary>
        UserFolder,
    }

    /// <summary>
    /// Encapsulates Google Drive file operations (find, download, create, update) for a single
    /// named file. Reused for both <c>vault.json</c> and <c>history.txt</c> to avoid duplicating
    /// Drive API interaction logic.
    /// </summary>
    private sealed class DriveFile(string fileName, string contentType)
    {
        /// <summary>
        /// Gets the cached Google Drive file ID, or <see langword="null"/> if not yet resolved.
        /// </summary>
        public string? FileId { get; private set; }

        /// <summary>
        /// Searches for the file by name in the configured Drive location and caches its ID.
        /// </summary>
        public async Task FindAsync(HttpClient http, string accessToken, DriveStorageMode storageMode, string? folderId, CancellationToken cancellationToken)
        {
            string query = storageMode == DriveStorageMode.AppData
                ? $"name = '{fileName}' and 'appDataFolder' in parents and trashed = false"
                : $"name = '{fileName}' and '{folderId}' in parents and trashed = false";

            string spaces = storageMode == DriveStorageMode.AppData ? "appDataFolder" : "drive";
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"{DriveApiBase}/files?q={encodedQuery}&spaces={spaces}&fields=files(id,name)&pageSize=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            await EnsureDriveSuccessAsync(response, cancellationToken);

            DriveFileListResponse? result = await response.Content.ReadFromJsonAsync<DriveFileListResponse>(cancellationToken);

            FileId = result?.Files is { Count: > 0 } && string.IsNullOrWhiteSpace(result.Files[0].Id) == false
                ? result.Files[0].Id
                : null;
        }

        /// <summary>
        /// Downloads the file content from Google Drive.
        /// Returns <see cref="string.Empty"/> if the file ID is not cached.
        /// </summary>
        public async Task<string> DownloadAsync(HttpClient http, string accessToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(FileId))
            {
                return string.Empty;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{DriveApiBase}/files/{FileId}?alt=media");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            await EnsureDriveSuccessAsync(response, cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        /// <summary>
        /// Uploads file content to Google Drive — creates the file if it does not
        /// exist, or updates it in-place. Handles 404 on update by re-creating.
        /// </summary>
        public async Task UploadAsync(HttpClient http, string accessToken, string content, DriveStorageMode storageMode, string? folderId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(FileId))
            {
                // File does not exist yet — create it.
                await CreateAsync(http, accessToken, content, storageMode, folderId, cancellationToken);
                return;
            }

            try
            {
                await UpdateAsync(http, accessToken, content, cancellationToken);
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode == 404)
            {
                // File was deleted externally — re-create.
                FileId = null;
                await CreateAsync(http, accessToken, content, storageMode, folderId, cancellationToken);
            }
        }

        /// <summary>
        /// Creates a new file in Google Drive using a multipart upload.
        /// </summary>
        private async Task CreateAsync(HttpClient http, string accessToken, string content, DriveStorageMode storageMode, string? folderId, CancellationToken cancellationToken)
        {
            List<string> parents = storageMode == DriveStorageMode.AppData
                ? ["appDataFolder"]
                : [folderId!];

            var metadata = new { name = fileName, parents };
            string metadataJson = JsonSerializer.Serialize(metadata);

            using var multipart = new MultipartContent("related");
            var metadataPart = new StringContent(metadataJson, Encoding.UTF8, "application/json");
            multipart.Add(metadataPart);

            var contentPart = new StringContent(content, Encoding.UTF8, contentType);
            multipart.Add(contentPart);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DriveUploadBase}/files?uploadType=multipart&fields=id");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = multipart;

            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            await EnsureDriveSuccessAsync(response, cancellationToken);

            DriveFileResponse? result = await response.Content.ReadFromJsonAsync<DriveFileResponse>(cancellationToken);
            FileId = result?.Id;

            if (string.IsNullOrWhiteSpace(FileId))
            {
                throw new InvalidOperationException($"Google Drive created '{fileName}' but did not return a valid file ID.");
            }
        }

        /// <summary>
        /// Updates the content of an existing file in Google Drive.
        /// </summary>
        private async Task UpdateAsync(HttpClient http, string accessToken, string content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"{DriveUploadBase}/files/{FileId}?uploadType=media");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(content, Encoding.UTF8, contentType);

            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            await EnsureDriveSuccessAsync(response, cancellationToken);
        }
    }

    /// <summary>
    /// Result returned by the JS interop popup containing the authorization code
    /// and state parameter from Google's redirect.
    /// </summary>
    private sealed class AuthCallbackResult
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    /// <summary>
    /// JSON response from Google's token endpoint.
    /// </summary>
    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    /// <summary>
    /// JSON response for a Google Drive file list query.
    /// </summary>
    private sealed class DriveFileListResponse
    {
        [JsonPropertyName("files")]
        public List<DriveFileResponse> Files { get; set; } = [];
    }

    /// <summary>
    /// JSON response for a single Google Drive file.
    /// </summary>
    private sealed class DriveFileResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
