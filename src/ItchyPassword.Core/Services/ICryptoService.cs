namespace ItchyPassword.Core.Services;

public interface ICryptoService
{
    Task<byte[]> EncryptV3Async(byte[] input, byte[] password);
    Task<byte[]> DecryptV3Async(byte[] input, byte[] password);
    Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart);
    Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose = "Password");
    Task<byte[]> GenerateRandomBytesAsync(int count);
}
