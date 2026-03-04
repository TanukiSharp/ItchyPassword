using ItchyPassword.Core.Services;
using System.Security.Cryptography;

namespace ItchyPassword.Core.Tests.Crypto;

/// <summary>
/// Pure .NET implementation of <see cref="ICryptoService"/> for testing only.
/// Mirrors the browser SubtleCrypto implementation (crypto.js) exactly.
/// NOT used in production — the Blazor app uses JS interop.
/// </summary>
public sealed class DotNetCryptoService : ICryptoService
{
    private const int SaltSize = 16;
    private const int IvSize = 12;
    private const int KeySize = 32; // 256-bit
    private const int TagSize = 16; // AES-GCM tag
    private const int IterationsContext = 400_000;

    public Task<byte[]> EncryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        // 1. Generate Nonce (IV) 12 bytes
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        // 2. Generate Salt 16 bytes
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // 3. Derive Key: PBKDF2-SHA512, 400k iter, 256 bits
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationsContext,
            HashAlgorithmName.SHA512,
            KeySize);

        var ciphertext = new byte[input.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(iv, input, ciphertext, tag);

        // Layout: [nonce 12] [salt 16] [ciphertext] [tag 16]
        // Note: WebCrypto usually appends tag to ciphertext. Does our js code do manual concatenation?
        // js: output.set(nonce, 0); output.set(salt, 12); output.set(encrypted, 28);
        // encrypted from WebCrypto includes tag at end.
        // So final blob is: Nonce || Salt || Ciphertext || Tag

        var result = new byte[IvSize + SaltSize + ciphertext.Length + TagSize];
        iv.CopyTo(result, 0);
        salt.CopyTo(result, IvSize);
        ciphertext.CopyTo(result, IvSize + SaltSize);
        tag.CopyTo(result, IvSize + SaltSize + ciphertext.Length);

        return Task.FromResult(result);
    }

    private Task<byte[]> DecryptAsync(byte[] input, byte[] password, int iterations)
    {
        // Layout: [nonce 12] [salt 16] [ciphertext...] [tag 16]
        if (input.Length < IvSize + SaltSize + TagSize)
            throw new CryptographicException("Input too short");

        var iv = input.AsSpan(0, IvSize).ToArray();
        var salt = input.AsSpan(IvSize, SaltSize).ToArray();

        int ciphertextLen = input.Length - (IvSize + SaltSize + TagSize);
        var ciphertext = input.AsSpan(IvSize + SaltSize, ciphertextLen).ToArray();
        var tag = input.AsSpan(input.Length - TagSize, TagSize).ToArray();

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            KeySize);

        var plaintext = new byte[ciphertextLen];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Task.FromResult(plaintext);
    }

    public async Task<byte[]> DecryptV2Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        return await DecryptAsync(input, password, 100_000);
    }

    public async Task<byte[]> DecryptV3Async(byte[] input, byte[] password, CancellationToken cancellationToken)
    {
        return await DecryptAsync(input, password, IterationsContext);
    }

    public Task<byte[]> GeneratePasswordV1Async(byte[] privatePart, byte[] publicPart, CancellationToken cancellationToken)
    {
        // Logic from crypto.js:
        // 1. Derive Key: PBKDF2(pass=private, salt=public, iter=100000, alg=SHA512, len=256 bits)
        // 2. HMAC-SHA512(Key, "Password")

        var key = Rfc2898DeriveBytes.Pbkdf2(
            privatePart,
            publicPart,
            100_000,
            HashAlgorithmName.SHA512,
            KeySize);

        var hmac = HMACSHA512.HashData(key, System.Text.Encoding.UTF8.GetBytes("Password"));
        return Task.FromResult(hmac);
    }

    public Task<byte[]> GeneratePasswordV2Async(byte[] privatePart, byte[] publicPart, string purpose, CancellationToken cancellationToken)
    {
        // Logic from crypto.js:
        // 1. Derive Key: PBKDF2(pass=private, salt=public, iter=400000, alg=SHA512, len=256 bits)
        // 2. HMAC-SHA512(Key, purpose)

        var key = Rfc2898DeriveBytes.Pbkdf2(
            privatePart,
            publicPart,
            400_000,
            HashAlgorithmName.SHA512,
            KeySize);

        var hmac = HMACSHA512.HashData(key, System.Text.Encoding.UTF8.GetBytes(purpose));
        return Task.FromResult(hmac);
    }

    public Task<byte[]> GenerateRandomBytesAsync(int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(RandomNumberGenerator.GetBytes(count));
    }
}
