using System.Text.Json.Serialization;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

/// <summary>
/// Implements <see cref="IPasskeyService"/> using WebAuthn via JS interop and
/// the existing <see cref="ICryptoService"/> for AES-GCM wrapping of the master key.
/// </summary>
public class PasskeyService(
    IJSRuntime js,
    ILocalStorageService storage,
    ICryptoService crypto,
    IMasterKeyProvider masterKeyProvider
) : IPasskeyService
{
    // Local storage key names. Kept private to this service.
    private const string EnabledKey = "passkey_enabled";
    private const string CredentialIdKey = "passkey_credential_id";
    private const string WrappedMasterKeyKey = "passkey_wrapped_master_key";
    private const string EnrolledAtKey = "passkey_enrolled_at";
    private const string ExpirationDaysKey = "passkey_expiration_days";
    private const string UserIdKey = "passkey_user_id";

    /// <inheritdoc />
    public async Task<bool> IsSupportedAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await js.InvokeAsync<bool>("ItchyPassword.Passkey.isSupported", cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<PasskeyStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        string? enabled = await storage.GetItemAsync(EnabledKey, cancellationToken);
        if (string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase) == false)
        {
            return PasskeyStatus.NotEnabled;
        }

        string? credentialId = await storage.GetItemAsync(CredentialIdKey, cancellationToken);
        string? wrappedKey = await storage.GetItemAsync(WrappedMasterKeyKey, cancellationToken);
        string? enrolledAt = await storage.GetItemAsync(EnrolledAtKey, cancellationToken);

        // Treat enrollment as missing if any of the three pieces is gone.
        // Clear the remaining ones for hygiene (recovers from partial corruption).
        if (string.IsNullOrWhiteSpace(credentialId) ||
            string.IsNullOrWhiteSpace(wrappedKey) ||
            string.IsNullOrWhiteSpace(enrolledAt))
        {
            await ClearEnrollmentDataAsync(cancellationToken);
            return PasskeyStatus.Expired;
        }

        if (DateTimeOffset.TryParse(enrolledAt, out DateTimeOffset enrolledAtValue) == false)
        {
            await ClearEnrollmentDataAsync(cancellationToken);
            return PasskeyStatus.Expired;
        }

        int days = await GetExpirationDaysAsync(cancellationToken);
        DateTimeOffset expiresAt = enrolledAtValue.AddDays(days);

        if (DateTimeOffset.UtcNow >= expiresAt)
        {
            await ClearEnrollmentDataAsync(cancellationToken);
            return PasskeyStatus.Expired;
        }

        return PasskeyStatus.Ready;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetEnrolledAtAsync(CancellationToken cancellationToken)
    {
        string? enrolledAt = await storage.GetItemAsync(EnrolledAtKey, cancellationToken);
        if (DateTimeOffset.TryParse(enrolledAt, out DateTimeOffset value))
        {
            return value;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<int> GetExpirationDaysAsync(CancellationToken cancellationToken)
    {
        string? raw = await storage.GetItemAsync(ExpirationDaysKey, cancellationToken);
        if (int.TryParse(raw, out int days) && days >= IPasskeyService.MinExpirationDays && days <= IPasskeyService.MaxExpirationDays)
        {
            return days;
        }
        return IPasskeyService.DefaultExpirationDays;
    }

    /// <inheritdoc />
    public async Task SetExpirationDaysAsync(int days, CancellationToken cancellationToken)
    {
        if (days < IPasskeyService.MinExpirationDays || days > IPasskeyService.MaxExpirationDays)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, $"Expiration days must be between {IPasskeyService.MinExpirationDays} and {IPasskeyService.MaxExpirationDays}.");
        }
        await storage.SetItemAsync(ExpirationDaysKey, days.ToString(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PasskeyEnrollResult> EnrollAsync(CancellationToken cancellationToken)
    {
        if (masterKeyProvider.HasMasterKey == false)
        {
            return PasskeyEnrollResult.Failed("Master key is not available.");
        }

        try
        {
            // Stable per-browser user handle. New credentials registered with
            // the same user handle replace prior ones in the authenticator's
            // resident-key storage (per WebAuthn spec).
            byte[] userId = await GetOrCreateUserIdAsync(cancellationToken);

            // The JS function performs WebAuthn create and wraps the master key
            // internally. Sensitive wrap material stays inside JS.
            WrapResult wrapped = await js.InvokeAsync<WrapResult>(
                "ItchyPassword.Passkey.enrollAndWrap",
                cancellationToken,
                userId,
                "ItchyPassword",
                masterKeyProvider.MasterKey
            );

            if (wrapped.CredentialId is null || wrapped.WrappedMasterKey is null ||
                wrapped.CredentialId.Length == 0 || wrapped.WrappedMasterKey.Length == 0)
            {
                return PasskeyEnrollResult.Failed("Authenticator did not return credential or wrapped key.");
            }

            await storage.SetItemAsync(CredentialIdKey, Convert.ToBase64String(wrapped.CredentialId), cancellationToken);
            await storage.SetItemAsync(WrappedMasterKeyKey, Convert.ToBase64String(wrapped.WrappedMasterKey), cancellationToken);
            await storage.SetItemAsync(EnrolledAtKey, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
            await storage.SetItemAsync(EnabledKey, "true", cancellationToken);

            return PasskeyEnrollResult.Successful();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PasskeyEnrollResult.Failed(ExtractFriendlyMessage(ex));
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> UnlockAsync(CancellationToken cancellationToken)
    {
        string? credentialIdBase64 = await storage.GetItemAsync(CredentialIdKey, cancellationToken);
        string? wrappedBase64 = await storage.GetItemAsync(WrappedMasterKeyKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(credentialIdBase64) || string.IsNullOrWhiteSpace(wrappedBase64))
        {
            throw new InvalidOperationException("No passkey enrollment found.");
        }

        // The JS function performs WebAuthn get and unwraps the master key
        // internally. Sensitive wrap material stays inside JS.
        byte[] masterKey = await js.InvokeAsync<byte[]>(
            "ItchyPassword.Passkey.unlockAndUnwrap",
            cancellationToken,
            credentialIdBase64,
            wrappedBase64
        );

        if (masterKey is null || masterKey.Length == 0)
        {
            throw new InvalidOperationException("Failed to unwrap master key.");
        }

        return masterKey;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(CancellationToken cancellationToken)
    {
        await ClearEnrollmentDataAsync(cancellationToken);
        await storage.RemoveItemAsync(EnabledKey, cancellationToken);
        await storage.RemoveItemAsync(ExpirationDaysKey, cancellationToken);
        await storage.RemoveItemAsync(UserIdKey, cancellationToken);
    }

    /// <summary>
    /// Removes the per-enrollment data (credential id, wrapped key, timestamp)
    /// while keeping the opt-in flag and configuration intact, so that the
    /// next manual master key entry triggers automatic re-enrollment.
    /// </summary>
    private async Task ClearEnrollmentDataAsync(CancellationToken cancellationToken)
    {
        await storage.RemoveItemAsync(CredentialIdKey, cancellationToken);
        await storage.RemoveItemAsync(WrappedMasterKeyKey, cancellationToken);
        await storage.RemoveItemAsync(EnrolledAtKey, cancellationToken);
    }

    /// <summary>
    /// Returns the persisted user handle, creating one on first use.
    /// </summary>
    private async Task<byte[]> GetOrCreateUserIdAsync(CancellationToken cancellationToken)
    {
        string? existing = await storage.GetItemAsync(UserIdKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(existing) == false)
        {
            try
            {
                return Convert.FromBase64String(existing);
            }
            catch (FormatException)
            {
                // Corrupted entry; regenerate below.
            }
        }

        byte[] newId = await crypto.GenerateRandomBytesAsync(16, cancellationToken);
        await storage.SetItemAsync(UserIdKey, Convert.ToBase64String(newId), cancellationToken);
        return newId;
    }

    /// <summary>
    /// Maps WebAuthn / DOM exception messages to short, user-readable text.
    /// </summary>
    private static string ExtractFriendlyMessage(Exception ex)
    {
        string message = ex.Message ?? string.Empty;

        if (message.Contains("PRF extension required", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("did not return the PRF derived key", StringComparison.OrdinalIgnoreCase))
        {
            return "Your device or browser does not support the secure cryptographic features (PRF) required for passkeys.";
        }
        if (message.Contains("NotAllowedError", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not allowed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "Passkey verification was cancelled, timed out, or denied by the authenticator.";
        }
        if (message.Contains("NotSupportedError", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            return "This device does not support passkey quick unlock in this browser.";
        }
        if (message.Contains("InvalidStateError", StringComparison.Ordinal))
        {
            return "A passkey is already registered for this app on this device.";
        }
        if (message.Contains("SecurityError", StringComparison.Ordinal))
        {
            return "Security restrictions prevented the passkey operation.";
        }

        return string.IsNullOrWhiteSpace(message) ? "Passkey operation failed." : message;
    }

    /// <summary>
    /// Shape of the JSON object returned by <c>ItchyPassword.Passkey.enrollAndWrap</c>.
    /// </summary>
    private sealed class WrapResult
    {
        [JsonPropertyName("credentialId")]
        public byte[]? CredentialId { get; set; }

        [JsonPropertyName("wrappedMasterKey")]
        public byte[]? WrappedMasterKey { get; set; }
    }
}
