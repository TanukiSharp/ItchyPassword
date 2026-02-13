using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text;

namespace ItchyPassword.App.Services;

public interface ICryptoService
{
    Task<byte[]> GeneratePasswordAsync(byte[] privateKey, byte[] publicKey, string purpose, int iterations);
    Task<byte[]> EncryptAsync(byte[] data, byte[] key, int iterations);
    Task<byte[]> DecryptAsync(byte[] data, byte[] key, int iterations);
}

public class CryptoService : ICryptoService, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public CryptoService(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./src/crypto.js").AsTask());
    }

    public async Task<byte[]> GeneratePasswordAsync(byte[] privateKey, byte[] publicKey, string purpose, int iterations)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<byte[]>("generatePassword", privateKey, publicKey, iterations);
    }

    public async Task<byte[]> EncryptAsync(byte[] data, byte[] key, int iterations)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<byte[]>("encrypt", data, key, iterations);
    }

    public async Task<byte[]> DecryptAsync(byte[] data, byte[] key, int iterations)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<byte[]>("decrypt", data, key, iterations);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
