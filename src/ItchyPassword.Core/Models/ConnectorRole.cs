namespace ItchyPassword.Core.Models;

/// <summary>
/// Defines the role of a vault connector in the current session.
/// </summary>
public enum ConnectorRole
{
    /// <summary>
    /// The connector is not used for reading or writing.
    /// </summary>
    Disabled,

    /// <summary>
    /// The connector is used as a backup writer only. It must have write access.
    /// </summary>
    Backup,

    /// <summary>
    /// The connector is the primary vault — used for both reading and writing.
    /// Exactly one connector can be Main at any time.
    /// </summary>
    Main,
}
