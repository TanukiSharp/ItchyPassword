namespace ItchyPassword.Core.Services;

/// <summary>
/// Provides access to the browser's persistent Local Storage.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Saves a key-value pair to local storage.
    /// </summary>
    Task SetItemAsync(string key, string value);

    /// <summary>
    /// Retrieves a value from local storage by key.
    /// </summary>
    Task<string?> GetItemAsync(string key);

    /// <summary>
    /// Removes an item from local storage by key.
    /// </summary>
    Task RemoveItemAsync(string key);
}
