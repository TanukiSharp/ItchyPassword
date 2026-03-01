using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

public class CryptoService : ICryptoService
{
    private readonly IJSRuntime _js;

    public CryptoService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<byte[]> EncryptV3Async(byte[] input, byte[] password)
    {
        return await _js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.encryptV3",
            input, password);
    }

    public async Task<byte[]> DecryptV3Async(byte[] input, byte[] password)
    {
        try
        {
            return await _js.InvokeAsync<byte[]>(
                "ItchyPassword.Crypto.decryptV3",
                input, password);
        }
        catch (Exception ex) when (ex.Message.Contains("OperationError") || ex.Message.Contains("decrypt"))
        {
             // JS 'OperationError' usually means decryption failed (wrong key/tag).
            throw new ItchyPassword.Core.Exceptions.VaultDecryptionException("Decryption failed.", ex);
        }
    }

    public async Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart)
    {
        return await _js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV1",
            privatePart, publicPart);
    }

    public async Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose = "Password")
    {
        return await _js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV2",
            privatePart, publicPart, purpose);
    }

    public async Task<byte[]> GenerateRandomBytesAsync(int count)
    {
        return await _js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generateRandomBytes",
            count);
    }
}
