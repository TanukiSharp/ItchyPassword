using System.Security.Cryptography;
using System.Text;

namespace ItchyPassword.App.Services;

public interface ICryptoService
{
    byte[] GeneratePassword(byte[] privateKey, byte[] publicKey, string purpose, int iterations);
    byte[] Encrypt(byte[] data, byte[] key, int iterations);
    byte[] Decrypt(byte[] data, byte[] key, int iterations);
}

public class CryptoService : ICryptoService
{
    public byte[] GeneratePassword(byte[] privateKey, byte[] publicKey, string purpose, int iterations)
    {
         if (privateKey is null || privateKey.Length == 0)
         {
             throw new ArgumentNullException(nameof(privateKey));
         }

         if (publicKey is null || publicKey.Length < 8)
         {
             throw new ArgumentException("Public key must be at least 8 bytes", nameof(publicKey));
         }

         using var algorithm = new Rfc2898DeriveBytes(privateKey, publicKey, iterations, HashAlgorithmName.SHA512);
         using var hkdfAlgorithm = new HMACSHA512(algorithm.GetBytes(32));
         return hkdfAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(purpose));
    }

    public byte[] Encrypt(byte[] data, byte[] key, int iterations)
    {
        const int NonceLength = 12;
        const int TagLength = 16;
        const int SaltLength = 16;
        
        byte[] output = new byte[NonceLength + SaltLength + data.Length + TagLength];
        
        Span<byte> outputSpan = output;
        Span<byte> salt = outputSpan.Slice(NonceLength, SaltLength);
        RandomNumberGenerator.Fill(salt);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(key, salt.ToArray(), iterations, HashAlgorithmName.SHA512);
        byte[] derivedKey = pbkdf2.GetBytes(32);
        
        using var aes = new AesGcm(derivedKey);
        
        Span<byte> nonce = outputSpan.Slice(0, NonceLength);
        RandomNumberGenerator.Fill(nonce);
        
        aes.Encrypt(nonce, data, outputSpan.Slice(NonceLength + SaltLength, data.Length), outputSpan.Slice(NonceLength + SaltLength + data.Length, TagLength));
        
        return output;
    }

    public byte[] Decrypt(byte[] data, byte[] key, int iterations)
    {
        const int NonceLength = 12;
        const int TagLength = 16;
        const int SaltLength = 16;
        
        if (data.Length < NonceLength + TagLength + SaltLength) throw new ArgumentException("Invalid data length");

        ReadOnlySpan<byte> inputSpan = data;
        ReadOnlySpan<byte> salt = inputSpan.Slice(NonceLength, SaltLength);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(key, salt.ToArray(), iterations, HashAlgorithmName.SHA512);
        byte[] derivedKey = pbkdf2.GetBytes(32);
        
        using var aes = new AesGcm(derivedKey);
        
        ReadOnlySpan<byte> nonce = inputSpan.Slice(0, NonceLength);
        ReadOnlySpan<byte> tag = inputSpan.Slice(data.Length - TagLength, TagLength);
        ReadOnlySpan<byte> ciphertext = inputSpan.Slice(NonceLength + SaltLength, data.Length - (NonceLength + SaltLength + TagLength));
        
        byte[] output = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, output);
        
        return output;
    }
}
