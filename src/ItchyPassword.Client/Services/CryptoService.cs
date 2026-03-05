using ItchyPassword.Core.Exceptions;
using ItchyPassword.Core.Services;
using Microsoft.JSInterop;

namespace ItchyPassword.Client.Services;

/// <summary>
/// Implements <see cref="ICryptoService"/> via <see cref="IJSRuntime"/> interop with SubtleCrypto.
/// </summary>
public class CryptoService(IJSRuntime js) : ICryptoService
{
    public async Task<byte[]> EncryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.encryptV3",
            cancellationToken, input, password
        );
    }

    public async Task<byte[]> DecryptV2Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        try
        {
            return await js.InvokeAsync<byte[]>(
                "ItchyPassword.Crypto.decryptV2",
                cancellationToken, input, password
            );
        }
        catch (Exception ex) when (ex.Message.Contains("OperationError") || ex.Message.Contains("decrypt"))
        {
             // JS 'OperationError' usually means decryption failed (wrong key/tag).
            throw new VaultDecryptionException("Decryption failed.", ex);
        }
    }

    public async Task<byte[]> DecryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        try
        {
            return await js.InvokeAsync<byte[]>(
                "ItchyPassword.Crypto.decryptV3",
                cancellationToken, input, password
            );
        }
        catch (Exception ex) when (ex.Message.Contains("OperationError") || ex.Message.Contains("decrypt"))
        {
             // JS 'OperationError' usually means decryption failed (wrong key/tag).
            throw new VaultDecryptionException("Decryption failed.", ex);
        }
    }

    public async Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV1",
            cancellationToken, privatePart, publicPart
        );
    }

    public async Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose = "Password", CancellationToken cancellationToken = default)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generatePasswordV2",
            cancellationToken, privatePart, publicPart, purpose
        );
    }

    public async Task<byte[]> GenerateRandomBytesAsync(int count, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.generateRandomBytes",
            cancellationToken, count
        );
    }

    public async Task<byte[]> ComputeHmacSha512Async(byte[] data, byte[] key, CancellationToken cancellationToken)
    {
        return await js.InvokeAsync<byte[]>(
            "ItchyPassword.Crypto.computeHmacSha512",
            cancellationToken, data, key
        );
    }
}
