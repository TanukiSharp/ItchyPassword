using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

public class LocalStorageService(IJSRuntime js) : ILocalStorageService
{
    /// <summary>
    /// Prefix applied to all localStorage keys to avoid collisions with other apps on the same origin (e.g. GitHub Pages).
    /// </summary>
    private const string KeyPrefix = "itchypassword_";

    public async Task SetItemAsync(string key, string value, CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, $"{KeyPrefix}{key}", value);
    }

    public async Task<string?> GetItemAsync(string key, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, $"{KeyPrefix}{key}");
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, $"{KeyPrefix}{key}");
    }
}
