using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using System.Security.Cryptography;
namespace ItchyPassword.Core.Tests.Services;

public class VaultCryptoServiceTests
{
    private readonly DotNetCryptoService _crypto;
    private readonly VaultCryptoService _service;
    private readonly byte[] _masterKey;

    public VaultCryptoServiceTests()
    {
        _crypto = new DotNetCryptoService();
        _service = new VaultCryptoService(_crypto);
        _masterKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
    }

    [Fact]
    public async Task EncryptSecretAsync_WithValidInput_ReturnsSecretData()
    {
        string plaintext = "Hello World";
        var result = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Cipher));
        Assert.Equal(SecretDataConstants.LatestCryptoVersion, result.CryptoVersion);
        Assert.Equal(SecretDataConstants.LatestEncoding, result.Encoding);
    }

    [Fact]
    public async Task EncryptSecretAsync_WithBase62_ThrowsNotSupportedException()
    {
        string plaintext = "Hello World";

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.EncryptSecretAsync(plaintext, _masterKey, "base62", CancellationToken.None));
    }

    [Fact]
    public async Task DecryptSecretAsync_WithValidCipher_ReturnsOriginalPlaintext()
    {
        string plaintext = "Sensitive Data 123";
        var secretData = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);

        string decrypted = await _service.DecryptSecretAsync(secretData, _masterKey, CancellationToken.None);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task DecryptSecretAsync_WithWrongKey_ThrowsOrReturnsGarbage()
    {
        string plaintext = "Sensitive Data 123";
        var secretData = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);

        byte[] wrongKey = new byte[_masterKey.Length];
        Array.Copy(_masterKey, wrongKey, _masterKey.Length);
        wrongKey[0] ^= 0xFF; // Flip bits

        // AES-GCM tag verification failure usually throws CryptographicException
        await Assert.ThrowsAnyAsync<CryptographicException>(() =>
            _service.DecryptSecretAsync(secretData, wrongKey, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateStaticKeyAsync_ReturnsDeterministicValue()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 16,
            Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789",
            EncodingVersion = 1, // BaseN.EncodeOneWay
            CryptoVersion = 1    // PBKDF2-SHA256
        };

        string result1 = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);
        string result2 = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal(result1, result2);
        Assert.Equal(16, result1.Length);

        // Ensure alphabet constraint
        foreach (char c in result1)
        {
            Assert.Contains(c, data.Alphabet);
        }
    }

    [Fact]
    public async Task GenerateStaticKeyAsync_V2_ReturnsDeterministicValue()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 20,
            Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            EncodingVersion = StaticKeyDataConstants.LatestEncodingVersion,
            CryptoVersion = StaticKeyDataConstants.LatestCryptoVersion,
        };

        string result1 = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);
        string result2 = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal(result1, result2);
        Assert.Equal(20, result1.Length);
    }
}
