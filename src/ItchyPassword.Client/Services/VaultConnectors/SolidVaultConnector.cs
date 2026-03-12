using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItchyPassword.Client.Services.VaultConnectors;

/// <summary>
/// Vault connector for SOLID (Social Linked Data) pods.
/// </summary>
/// <remarks>
/// Authentication uses Solid-OIDC — OpenID Connect extended with DPoP (RFC 9449) token binding.
/// On first sign-in, the connector attempts dynamic client registration; for providers that do not
/// expose a <c>registration_endpoint</c>, the user must supply a pre-registered Client ID in Settings.
///
/// The ES256 DPoP key pair is ephemeral: it is generated fresh per session in the browser's
/// SubtleCrypto layer, is non-extractable, and is never persisted anywhere.
///
/// Pod resources are read and written using standard HTTP GET / PUT with
/// <c>Authorization: DPoP &lt;token&gt;</c> and a fresh <c>DPoP: &lt;proof&gt;</c> header on every
/// request.
/// </remarks>
public class SolidVaultConnector(
    HttpClient http,
    ILocalStorageService storage,
    ICryptoService crypto,
    IMasterKeyProvider masterKeyProvider,
    IJSRuntime js
) : IVaultConnector
{
    // -------------------------------------------------------------------------
    // Configuration entry keys
    // -------------------------------------------------------------------------

    private const string IssuerUrlKey = "IssuerUrl";
    private const string VaultFileUrlKey = "VaultFileUrl";
    private const string ClientIdKey = "ClientId";

    // -------------------------------------------------------------------------
    // localStorage keys (not exposed through Configuration — not UI-visible)
    // -------------------------------------------------------------------------

    private const string AccessTokenStorageKey = "solid_access_token";
    private const string RefreshTokenStorageKey = "solid_refresh_token";

    private const string OAuthCallbackPath = "solid-oauth-callback";

    // -------------------------------------------------------------------------
    // Instance state
    // -------------------------------------------------------------------------

    // Cached OIDC discovery document for the current issuer.
    private OidcDiscovery? _discovery;

    // Most recently received DPoP-Nonce header value (from AS or RS).
    private string? _dpopNonce;

    // In-memory access and refresh tokens.
    private string _accessToken = string.Empty;
    private string _refreshToken = string.Empty;

    // Guards against redundant config-load calls.
    private bool _configLoaded;

    // -------------------------------------------------------------------------
    // IVaultConnector identity
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public Guid Id { get; } = Guid.Parse("f3e2d1c0-b9a8-7654-3210-fedcba987654");

    /// <inheritdoc />
    public string Name { get; } = "SOLID Pod";

    /// <inheritdoc />
    public string Description { get; } = "Store vault in a SOLID pod. Supports any Solid-OIDC provider.";

    // -------------------------------------------------------------------------
    // Configuration schema
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationEntry> Configuration { get; } =
    [
        new ConfigurationEntry
        {
            Key = IssuerUrlKey,
            Label = "Provider URL",
            Description = "The OIDC identity provider base URL (e.g. https://login.inrupt.com).",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "https://login.inrupt.com",
            StorageKey = "solid_issuer_url",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = VaultFileUrlKey,
            Label = "Vault File URL",
            Description = "The full URL of the vault file in your pod (e.g. https://storage.inrupt.com/e283fa34-d03c-4fb6-9373-8535144e7ce2/ItchyPassword/vault.json).",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "https://storage.inrupt.com/e283fa34-d03c-4fb6-9373-8535144e7ce2/ItchyPassword/vault.json",
            StorageKey = "solid_vault_file_url",
            IsRequired = true,
        },
        new ConfigurationEntry
        {
            Key = ClientIdKey,
            Label = "Client ID",
            Description = "Leave empty to let ItchyPassword register automatically. Fill in only if your provider requires a pre-registered client.",
            Kind = ConfigurationEntryKind.Text,
            Placeholder = "(auto-registered)",
            StorageKey = "solid_client_id",
            IsRequired = false,
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
    public bool CanRetryAccess
    {
        get
        {
            // Interactive sign-in uses a popup and requires a user gesture; allow retry.
            return true;
        }
    }

    /// <inheritdoc />
    public string? AccessFailureMessage { get; private set; }

    // -------------------------------------------------------------------------
    // Secrets lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Clears in-memory OAuth tokens.
    /// </summary>
    public Task ClearSecretsAsync()
    {
        _accessToken = string.Empty;
        _refreshToken = string.Empty;
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Configuration persistence
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        if (_configLoaded)
        {
            return;
        }

        byte[]? masterKey = masterKeyProvider.HasMasterKey ? masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.LoadEntriesAsync(Configuration, storage, masterKey, crypto, cancellationToken);

        _accessToken = await LoadEncryptedTokenAsync(AccessTokenStorageKey, cancellationToken);
        _refreshToken = await LoadEncryptedTokenAsync(RefreshTokenStorageKey, cancellationToken);

        _configLoaded = true;
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        byte[]? masterKey = masterKeyProvider.HasMasterKey ? masterKeyProvider.MasterKey : null;
        await VaultConnectorHelper.SaveEntriesAsync(Configuration, storage, masterKey, crypto, cancellationToken);

        await SaveEncryptedTokenAsync(AccessTokenStorageKey, _accessToken, cancellationToken);
        await SaveEncryptedTokenAsync(RefreshTokenStorageKey, _refreshToken, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Access
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<bool> AccessAsync(CancellationToken cancellationToken)
    {
        AccessFailureMessage = null;

        await LoadConfigurationAsync(cancellationToken);

        // Reload tokens in case another tab updated them.
        try
        {
            _accessToken = await LoadEncryptedTokenAsync(AccessTokenStorageKey, cancellationToken);
            _refreshToken = await LoadEncryptedTokenAsync(RefreshTokenStorageKey, cancellationToken);
        }
        catch
        {
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
        }

        // 1. Use the existing access token if still valid (client-side exp check).
        if (string.IsNullOrWhiteSpace(_accessToken) == false && IsTokenValid(_accessToken))
        {
            return true;
        }

        // 2. Try silently refreshing using the stored refresh token.
        if (string.IsNullOrWhiteSpace(_refreshToken) == false)
        {
            try
            {
                string? refreshed = await TryRefreshTokenAsync(_refreshToken, cancellationToken);

                if (string.IsNullOrWhiteSpace(refreshed) == false)
                {
                    _accessToken = refreshed;
                    await SaveConfigurationAsync(cancellationToken);
                    return true;
                }
            }
            catch
            {
                // A failed refresh is non-fatal; fall through to interactive sign-in.
            }

            _accessToken = string.Empty;
            _refreshToken = string.Empty;
        }

        // 3. Interactive sign-in via popup.
        return await InteractiveSignInAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Vault read / write
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<string> LoadVaultAsync(CancellationToken cancellationToken)
    {
        await LoadConfigurationAsync(cancellationToken);

        string vaultUrl = VaultConnectorHelper.GetValue(Configuration, VaultFileUrlKey);
        string accessToken = await GetOrRefreshTokenAsync(cancellationToken);

        using HttpResponseMessage response = await SendDpopAsync(
            HttpMethod.Get, vaultUrl, bodyFactory: null, accessToken, cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // No vault file yet — return empty so the app creates a new vault.
            return string.Empty;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveVaultAsync(string content, string changeHint, CancellationToken cancellationToken)
    {
        await LoadConfigurationAsync(cancellationToken);

        string vaultUrl = VaultConnectorHelper.GetValue(Configuration, VaultFileUrlKey);
        string accessToken = await GetOrRefreshTokenAsync(cancellationToken);

        using HttpResponseMessage response = await SendDpopAsync(
            HttpMethod.Put,
            vaultUrl,
            () => new StringContent(content, Encoding.UTF8, "application/json"),
            accessToken,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();
    }

    // -------------------------------------------------------------------------
    // OIDC discovery
    // -------------------------------------------------------------------------

    private async Task<OidcDiscovery> EnsureDiscoveryAsync(CancellationToken cancellationToken)
    {
        if (_discovery is not null)
        {
            return _discovery;
        }

        string issuer = VaultConnectorHelper.GetValue(Configuration, IssuerUrlKey).TrimEnd('/');
        string discoveryUrl = $"{issuer}/.well-known/openid-configuration";

        using HttpResponseMessage response = await http.GetAsync(discoveryUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        _discovery = await response.Content.ReadFromJsonAsync<OidcDiscovery>(cancellationToken)
            ?? throw new InvalidOperationException("SOLID provider returned an empty discovery document.");

        return _discovery;
    }

    // -------------------------------------------------------------------------
    // Client ID resolution — user-configured or dynamic registration
    // -------------------------------------------------------------------------

    private async Task<string> ResolveClientIdAsync(OidcDiscovery discovery, string redirectUri, CancellationToken cancellationToken)
    {
        // 1. User has configured a client ID explicitly.
        string configured = VaultConnectorHelper.GetValue(Configuration, ClientIdKey);

        if (string.IsNullOrWhiteSpace(configured) == false)
        {
            return configured;
        }

        // 2. An auto-registered client ID was stored in a previous session.
        string issuer = VaultConnectorHelper.GetValue(Configuration, IssuerUrlKey).TrimEnd('/');
        string cacheKey = $"solid_auto_client_{ComputeShortHash(issuer)}";
        string? cached = await storage.GetItemAsync(cacheKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(cached) == false)
        {
            return cached;
        }

        // 3. Attempt dynamic client registration (RFC 7591).
        if (string.IsNullOrWhiteSpace(discovery.RegistrationEndpoint))
        {
            throw new InvalidOperationException(
                "This SOLID provider does not support dynamic client registration. " +
                "Please enter a Client ID in Settings."
            );
        }

        var registrationBody = new
        {
            application_type = "web",
            client_name = "ItchyPassword",
            redirect_uris = new[] { redirectUri },
            token_endpoint_auth_method = "none",
            grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" },
            scope = "openid offline_access webid",
        };

        string requestJson = JsonSerializer.Serialize(registrationBody);

        using HttpResponseMessage regResponse = await http.PostAsync(
            discovery.RegistrationEndpoint,
            new StringContent(requestJson, Encoding.UTF8, "application/json"),
            cancellationToken
        );

        regResponse.EnsureSuccessStatusCode();

        ClientRegistrationResponse? reg = await regResponse.Content.ReadFromJsonAsync<ClientRegistrationResponse>(cancellationToken);

        string clientId = reg?.ClientId
            ?? throw new InvalidOperationException("Dynamic registration succeeded but the provider returned no client_id.");

        // Persist so we reuse it across sessions (not sensitive — client IDs are public).
        await storage.SetItemAsync(cacheKey, clientId, cancellationToken);

        return clientId;
    }

    // -------------------------------------------------------------------------
    // Interactive sign-in
    // -------------------------------------------------------------------------

    private async Task<bool> InteractiveSignInAsync(CancellationToken cancellationToken)
    {
        OidcDiscovery discovery;

        try
        {
            discovery = await EnsureDiscoveryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            AccessFailureMessage = $"Could not reach the SOLID provider: {ex.Message}";
            return false;
        }

        string redirectUri = ComputeRedirectUri();
        string clientId;

        try
        {
            clientId = await ResolveClientIdAsync(discovery, redirectUri, cancellationToken);
        }
        catch (Exception ex)
        {
            AccessFailureMessage = $"Client registration failed: {ex.Message}";
            return false;
        }

        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = ComputeCodeChallenge(codeVerifier);
        string state = GenerateState();

        // dpop_jkt binds the authorization request to the current DPoP key (Solid-OIDC 1.0).
        string dpopJkt = await js.InvokeAsync<string>("solidInterop.getDpopKeyThumbprint", cancellationToken);

        string authUrl = BuildAuthorizationUrl(
            discovery.AuthorizationEndpoint, clientId, redirectUri, codeChallenge, state, dpopJkt
        );

        // Open the popup synchronously to preserve the browser's transient user activation.
        if (js is IJSInProcessRuntime jsInProcess)
        {
            jsInProcess.InvokeVoid("solidInterop.openPopup", authUrl);
        }

        string? resultJson = await js.InvokeAsync<string?>("solidInterop.awaitResult", cancellationToken);

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            AccessFailureMessage = "SOLID sign-in was cancelled or failed. Click Retry to try again.";
            return false;
        }

        AuthCallbackResult? callback = JsonSerializer.Deserialize<AuthCallbackResult>(resultJson);

        if (callback is null || string.IsNullOrWhiteSpace(callback.Code))
        {
            AccessFailureMessage = "SOLID sign-in returned invalid data. Click Retry to try again.";
            return false;
        }

        // Validate state to prevent CSRF attacks.
        if (callback.State != state)
        {
            AccessFailureMessage = "SOLID sign-in state mismatch (possible CSRF). Click Retry to try again.";
            return false;
        }

        TokenResponse? tokens = await ExchangeCodeAsync(
            discovery.TokenEndpoint, clientId, callback.Code, codeVerifier, redirectUri, cancellationToken
        );

        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            AccessFailureMessage = "Failed to obtain an access token from the SOLID provider. Click Retry to try again.";
            return false;
        }

        _accessToken = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken ?? string.Empty;

        await SaveConfigurationAsync(cancellationToken);

        return true;
    }

    // -------------------------------------------------------------------------
    // Token exchange and refresh
    // -------------------------------------------------------------------------

    private async Task<TokenResponse?> ExchangeCodeAsync(
        string tokenEndpoint,
        string clientId,
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken
    )
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
        };

        return await PostTokenRequestAsync(tokenEndpoint, parameters, cancellationToken);
    }

    private async Task<string?> TryRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        OidcDiscovery discovery = await EnsureDiscoveryAsync(cancellationToken);
        string redirectUri = ComputeRedirectUri();
        string clientId = await ResolveClientIdAsync(discovery, redirectUri, cancellationToken);

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
        };

        TokenResponse? tokens = await PostTokenRequestAsync(discovery.TokenEndpoint, parameters, cancellationToken);

        // Some servers rotate the refresh token; update ours if a new one was issued.
        if (tokens?.RefreshToken is { Length: > 0 })
        {
            _refreshToken = tokens.RefreshToken;
        }

        return tokens?.AccessToken;
    }

    /// <summary>
    /// POSTs a token request with a DPoP proof, retrying once when the server
    /// requests a nonce it did not previously supply.
    /// </summary>
    private async Task<TokenResponse?> PostTokenRequestAsync(
        string tokenEndpoint,
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken
    )
    {
        string baseUrl = StripQueryAndFragment(tokenEndpoint);

        string dpopProof = await js.InvokeAsync<string>(
            "solidInterop.buildDpopProof",
            cancellationToken, "POST", baseUrl, null, _dpopNonce
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(parameters),
        };
        request.Headers.TryAddWithoutValidation("DPoP", dpopProof);

        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        UpdateDpopNonce(response);

        // Retry once if the AS requested a DPoP nonce we did not possess.
        if (response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.Contains("DPoP-Nonce"))
        {
            string retryProof = await js.InvokeAsync<string>(
                "solidInterop.buildDpopProof",
                cancellationToken, "POST", baseUrl, null, _dpopNonce
            );

            using var retryRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(parameters),
            };
            retryRequest.Headers.TryAddWithoutValidation("DPoP", retryProof);

            using HttpResponseMessage retryResponse = await http.SendAsync(retryRequest, cancellationToken);
            UpdateDpopNonce(retryResponse);

            if (retryResponse.IsSuccessStatusCode == false)
            {
                return null;
            }

            return await retryResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        }

        if (response.IsSuccessStatusCode == false)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Authenticated HTTP with DPoP
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the current access token, or silently refreshes it if expired.
    /// Throws if no valid token is available (requires interactive sign-in).
    /// </summary>
    private async Task<string> GetOrRefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_accessToken) == false)
        {
            return _accessToken;
        }

        if (string.IsNullOrWhiteSpace(_refreshToken) == false)
        {
            string? refreshed = await TryRefreshTokenAsync(_refreshToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshed) == false)
            {
                _accessToken = refreshed;
                await SaveConfigurationAsync(cancellationToken);
                return _accessToken;
            }
        }

        throw new InvalidOperationException("No valid SOLID access token. Use 'Test access' in Settings to sign in.");
    }

    /// <summary>
    /// Sends an authenticated HTTP request with a DPoP proof, retrying once on a
    /// DPoP nonce challenge from the resource server.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="url">Full target URL.</param>
    /// <param name="bodyFactory">Optional factory that creates the request body. Called again on retry.</param>
    /// <param name="accessToken">The current access token.</param>
    private async Task<HttpResponseMessage> SendDpopAsync(
        HttpMethod method,
        string url,
        Func<HttpContent>? bodyFactory,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        string baseUrl = StripQueryAndFragment(url);

        HttpResponseMessage response = await SendDpopOnceAsync(
            method, url, baseUrl, bodyFactory, accessToken, cancellationToken
        );

        // If the resource server sends a new DPoP-Nonce with a 401, retry once.
        if (response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.Contains("DPoP-Nonce"))
        {
            response.Dispose();
            response = await SendDpopOnceAsync(
                method, url, baseUrl, bodyFactory, accessToken, cancellationToken
            );
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendDpopOnceAsync(
        HttpMethod method,
        string url,
        string baseUrl,
        Func<HttpContent>? bodyFactory,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        string dpopProof = await js.InvokeAsync<string>(
            "solidInterop.buildDpopProof",
            cancellationToken, method.Method, baseUrl, accessToken, _dpopNonce
        );

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", accessToken);
        request.Headers.TryAddWithoutValidation("DPoP", dpopProof);

        if (bodyFactory is not null)
        {
            request.Content = bodyFactory();
        }

        HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        UpdateDpopNonce(response);

        return response;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void UpdateDpopNonce(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("DPoP-Nonce", out IEnumerable<string>? values))
        {
            _dpopNonce = values.FirstOrDefault();
        }
    }

    private static string BuildAuthorizationUrl(
        string authEndpoint,
        string clientId,
        string redirectUri,
        string codeChallenge,
        string state,
        string dpopJkt
    )
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid offline_access webid",
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["dpop_jkt"] = dpopJkt,
        };

        string queryString = string.Join("&", parameters
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
        );

        return $"{authEndpoint}?{queryString}";
    }

    private string ComputeRedirectUri()
    {
        return new Uri(http.BaseAddress!, OAuthCallbackPath).AbsoluteUri;
    }

    private static string GenerateCodeVerifier()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string GenerateState()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Removes the query string and fragment from a URL as required by the DPoP
    /// <c>htu</c> claim (RFC 9449 §4.2).
    /// </summary>
    private static string StripQueryAndFragment(string url)
    {
        int queryPos = url.IndexOf('?', StringComparison.Ordinal);
        int fragmentPos = url.IndexOf('#', StringComparison.Ordinal);

        int cutAt = (queryPos, fragmentPos) switch
        {
            (>= 0, >= 0) => Math.Min(queryPos, fragmentPos),
            (>= 0, _)    => queryPos,
            (_, >= 0)    => fragmentPos,
            _            => -1,
        };

        return cutAt >= 0 ? url[..cutAt] : url;
    }

    /// <summary>
    /// Returns a short hex string derived from the SHA-256 of <paramref name="input"/>.
    /// Used to build issuer-scoped localStorage keys without embedding raw URLs as keys.
    /// </summary>
    private static string ComputeShortHash(string input)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash)[..8];
    }

    /// <summary>
    /// Checks whether a JWT access token has a valid (unexpired) <c>exp</c> claim.
    /// Applies a 60-second clock-skew margin. Does not verify the signature.
    /// </summary>
    private static bool IsTokenValid(string token)
    {
        try
        {
            string[] parts = token.Split('.');

            if (parts.Length != 3)
            {
                return false;
            }

            // Re-pad the payload segment to a valid Base64 length.
            string payloadB64 = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            int padding = payloadB64.Length % 4;

            if (padding > 0)
            {
                payloadB64 += new string('=', 4 - padding);
            }

            byte[] payloadBytes = Convert.FromBase64String(payloadB64);

            using JsonDocument doc = JsonDocument.Parse(payloadBytes);

            if (doc.RootElement.TryGetProperty("exp", out JsonElement expElement))
            {
                long expiresAt = expElement.GetInt64();
                long nowWithMargin = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
                return nowWithMargin < expiresAt;
            }

            // No exp claim — treat as invalid to be safe.
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> LoadEncryptedTokenAsync(string key, CancellationToken cancellationToken)
    {
        string? stored = await storage.GetItemAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        try
        {
            return masterKeyProvider.HasMasterKey
                ? await VaultConnectorHelper.DecryptIfNeededAsync(stored, masterKeyProvider.MasterKey, crypto, cancellationToken)
                : stored;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt SOLID token '{key}'. The master key may be incorrect.", ex);
        }
    }

    private async Task SaveEncryptedTokenAsync(string key, string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value) || masterKeyProvider.HasMasterKey == false)
        {
            return;
        }

        string encrypted = await VaultConnectorHelper.EncryptAsync(value, masterKeyProvider.MasterKey, crypto, cancellationToken);

        await storage.SetItemAsync(key, encrypted, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // JSON response records (OIDC spec uses snake_case property names)
    // -------------------------------------------------------------------------

#pragma warning disable IDE1006 // Naming Styles

    private sealed record OidcDiscovery(
        [property: JsonPropertyName("authorization_endpoint")] string AuthorizationEndpoint,
        [property: JsonPropertyName("token_endpoint")] string TokenEndpoint,
        [property: JsonPropertyName("registration_endpoint")] string? RegistrationEndpoint);

    private sealed record ClientRegistrationResponse(
        [property: JsonPropertyName("client_id")] string ClientId);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn);

    private sealed record AuthCallbackResult(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("state")] string State);

#pragma warning restore IDE1006
}
