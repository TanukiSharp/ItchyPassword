namespace ItchyPassword.Core.Services;

/// <summary>
/// Represents the current state of the optional passkey quick-unlock feature
/// on this device.
/// </summary>
public enum PasskeyStatus
{
    /// <summary>
    /// The user has not opted in to passkey quick-unlock on this device.
    /// </summary>
    NotEnabled,

    /// <summary>
    /// The user has opted in but enrollment data is missing or expired.
    /// The next successful manual master key entry will trigger re-enrollment.
    /// </summary>
    Expired,

    /// <summary>
    /// Passkey enrollment is present and valid. Auto-unlock can be attempted.
    /// </summary>
    Ready
}
