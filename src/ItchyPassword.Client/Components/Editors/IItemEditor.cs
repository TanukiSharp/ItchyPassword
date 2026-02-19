namespace ItchyPassword.Client.Components.Editors;

/// <summary>
/// Common interface for vault item editors.
/// Called by parent pages to finalize item data before saving to the vault.
/// </summary>
public interface IItemEditor
{
    /// <summary>
    /// Prepares the item data for saving (e.g. encrypting secrets, capturing parameters).
    /// Returns true if the data is ready to save, false if validation failed.
    /// </summary>
    Task<bool> PrepareForSaveAsync();
}
