using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

public class LocalStorageService(IJSRuntime js) : ILocalStorageService
{
    public async Task SetItemAsync(string key, string value, CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);
    }

    public async Task<string?> GetItemAsync(string key, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);
    }

    public async Task RemoveItemAsync(string key, CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
    }
}
