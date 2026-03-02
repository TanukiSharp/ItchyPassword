using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

public class CryptoService(IJSRuntime js) : ICryptoService
{
    public async Task<byte[]> EncryptV3Async(byte[] input, byte[] password)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.encryptV3",
            input, password
        );
    }

    public async Task<byte[]> DecryptV2Async(byte[] input, byte[] password)
    {
        try
        {
            return await js.InvokeAsync<byte[]>(
                "ItchyPassword.Crypto.decryptV2",
                input, password
            );
        }
        catch (Exception ex) when (ex.Message.Contains("OperationError") || ex.Message.Contains("decrypt"))
        {
             // JS 'OperationError' usually means decryption failed (wrong key/tag).
            throw new VaultDecryptionException("Decryption failed.", ex);
        }
    }

    public async Task<byte[]> DecryptV3Async(byte[] input, byte[] password)
    {
        try
        {
            return await js.InvokeAsync<byte[]>(
                "ItchyPassword.Crypto.decryptV3",
                input, password
            );
        }
        catch (Exception ex) when (ex.Message.Contains("OperationError") || ex.Message.Contains("decrypt"))
        {
             // JS 'OperationError' usually means decryption failed (wrong key/tag).
            throw new VaultDecryptionException("Decryption failed.", ex);
        }
    }

    public async Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV1",
            privatePart, publicPart
        );
    }

    public async Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose = "Password")
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV2",
            privatePart, publicPart, purpose
        );
    }

    public async Task<byte[]> GenerateRandomBytesAsync(int count)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generateRandomBytes",
            count
        );
    }
}
