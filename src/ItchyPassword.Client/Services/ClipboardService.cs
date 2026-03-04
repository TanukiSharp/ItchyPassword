using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

public class ClipboardService(IJSRuntime jsRuntime)
{
    public async Task CopyTextAsync(string text, CancellationToken cancellationToken)
    {
        await jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", cancellationToken, text);
    }
}
