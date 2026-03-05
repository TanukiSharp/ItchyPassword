namespace ItchyPassword.Core.Services;

public interface ICryptoService
{
    Task<byte[]> EncryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken);
    Task<byte[]> DecryptV2Async(byte[] input, byte[] password, CancellationToken cancellationToken);
    Task<byte[]> DecryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken);
    Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart, CancellationToken cancellationToken);
    Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose, CancellationToken cancellationToken);
    Task<byte[]> GenerateRandomBytesAsync(int count, CancellationToken cancellationToken);
    Task<byte[]> ComputeHmacSha512Async(byte[] data, byte[] key, CancellationToken cancellationToken);
}
