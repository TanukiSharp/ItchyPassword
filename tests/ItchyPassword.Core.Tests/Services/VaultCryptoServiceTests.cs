using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Xunit;

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
        var result = await _service.EncryptSecretAsync(plaintext, _masterKey);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Cipher));
        Assert.Equal(3, result.CryptoVersion);
        Assert.Equal("base58", result.Encoding);
    }

    [Fact]
    public async Task EncryptSecretAsync_WithBase62_ReturnsCorrectEncoding()
    {
        string plaintext = "Hello World";
        var result = await _service.EncryptSecretAsync(plaintext, _masterKey, "base62");

        Assert.Equal("base62", result.Encoding);
        // Base62 alphabet specific check if needed, but integration test mostly.
    }

    [Fact]
    public async Task DecryptSecretAsync_WithValidCipher_ReturnsOriginalPlaintext()
    {
        string plaintext = "Sensitive Data 123";
        var secretData = await _service.EncryptSecretAsync(plaintext, _masterKey);

        string decrypted = await _service.DecryptSecretAsync(secretData, _masterKey);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task DecryptSecretAsync_WithWrongKey_ThrowsOrReturnsGarbage()
    {
        string plaintext = "Sensitive Data 123";
        var secretData = await _service.EncryptSecretAsync(plaintext, _masterKey);

        byte[] wrongKey = new byte[_masterKey.Length];
        Array.Copy(_masterKey, wrongKey, _masterKey.Length);
        wrongKey[0] ^= 0xFF; // Flip bits

        // AES-GCM tag verification failure usually throws CryptographicException
        await Assert.ThrowsAnyAsync<CryptographicException>(() =>
            _service.DecryptSecretAsync(secretData, wrongKey));
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

        string result1 = await _service.GenerateStaticKeyAsync(data, _masterKey);
        string result2 = await _service.GenerateStaticKeyAsync(data, _masterKey);

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
            EncodingVersion = 2, // BaseN.Encode
            CryptoVersion = 2    // HMAC-SHA512
        };

        string result1 = await _service.GenerateStaticKeyAsync(data, _masterKey);
        string result2 = await _service.GenerateStaticKeyAsync(data, _masterKey);

        Assert.Equal(result1, result2);
        Assert.Equal(20, result1.Length);
    }
}
