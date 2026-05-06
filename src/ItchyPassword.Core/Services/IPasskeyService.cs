using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Provides optional passkey-based quick unlock of the vault on this device.
///
/// The master key remains the sole vault decryption key. The passkey only
/// wraps (encrypts) the master key in local storage using a hardware-bound
/// PRF-derived wrapping key. The wrapped master key is stored in browser
/// local storage; the wrapping key never leaves the authenticator.
///
/// Each device has its own enrollment. Enrollment is per-browser-profile
/// because both the authenticator credential and the wrapped key live in
/// browser-scoped storage.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Default expiration window in days for a passkey enrollment.
    /// </summary>
    public const int DefaultExpirationDays = 7;

    /// <summary>
    /// Minimum allowed expiration window in days.
    /// </summary>
    public const int MinExpirationDays = 1;

    /// <summary>
    /// Maximum allowed expiration window in days.
    /// </summary>
    public const int MaxExpirationDays = 90;

    /// <summary>
    /// Returns true if this browser supports WebAuthn with the PRF extension
    /// and a user-verifying platform authenticator is available.
    /// </summary>
    Task<bool> IsSupportedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the current passkey state on this device.
    /// </summary>
    Task<PasskeyStatus> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the timestamp of the current enrollment, or null when no
    /// enrollment data is present.
    /// </summary>
    Task<DateTimeOffset?> GetEnrolledAtAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the configured expiration window in days. Defaults to 7.
    /// </summary>
    Task<int> GetExpirationDaysAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists the expiration window in days. Must be between 1 and 90.
    /// </summary>
    Task SetExpirationDaysAsync(int days, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new platform passkey, derives a wrapping key from the PRF
    /// output, encrypts the current master key from the master key provider,
    /// and stores the credential id, the wrapped master key, and the
    /// enrollment timestamp in local storage. Also sets the opt-in flag.
    /// </summary>
    Task<PasskeyEnrollResult> EnrollAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates with the stored credential, derives the wrapping key
    /// from the PRF output, and decrypts the wrapped master key.
    /// Returns the raw master key bytes.
    /// </summary>
    Task<byte[]> UnlockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Removes all passkey-related data including the opt-in flag.
    /// </summary>
    Task RemoveAsync(CancellationToken cancellationToken);
}
