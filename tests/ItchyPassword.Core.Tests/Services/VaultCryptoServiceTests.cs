using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Microsoft.Playwright;
using System.Security.Cryptography;

namespace ItchyPassword.Core.Tests.Services;

public class VaultCryptoServiceTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private PlaywrightCryptoService _crypto = null!;
    private VaultCryptoService _service = null!;
    private readonly byte[] _masterKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    public VaultCryptoServiceTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageWithCryptoAsync();
        _crypto = new PlaywrightCryptoService(_page);
        _service = new VaultCryptoService(_crypto);
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }
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
    public async Task DecryptSecretAsync_WithWrongKey_Throws()
    {
        string plaintext = "Sensitive Data 123";
        var secretData = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);

        byte[] wrongKey = new byte[_masterKey.Length];
        Array.Copy(_masterKey, wrongKey, _masterKey.Length);
        wrongKey[0] ^= 0xFF; // Flip bits.

        // AES-GCM tag verification fails: CryptographicException in .NET, PlaywrightException via browser.
        await Assert.ThrowsAnyAsync<Exception>(() =>
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

    // ── Input validation (pure C# logic, no crypto calls) ─────────────

    [Fact]
    public async Task EncryptSecretAsync_EmptyPlaintext_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EncryptSecretAsync(string.Empty, _masterKey, "base58", CancellationToken.None));
    }

    [Fact]
    public async Task EncryptSecretAsync_EmptyKey_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EncryptSecretAsync("test", [], "base58", CancellationToken.None));
    }

    [Fact]
    public async Task DecryptSecretAsync_EmptyCipher_Throws()
    {
        var secretData = new SecretDataV2
        {
            Cipher = string.Empty,
            CryptoVersion = SecretDataConstants.LatestCryptoVersion,
            Encoding = "base58",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DecryptSecretAsync(secretData, _masterKey, CancellationToken.None));
    }

    [Fact]
    public async Task DecryptSecretAsync_EmptyKey_Throws()
    {
        var secretData = new SecretDataV2
        {
            Cipher = "someCipher",
            CryptoVersion = SecretDataConstants.LatestCryptoVersion,
            Encoding = "base58",
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.DecryptSecretAsync(secretData, [], CancellationToken.None));
    }

    [Fact]
    public async Task GenerateStaticKeyAsync_UnsupportedCryptoVersion_Throws()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "test.com",
            Length = 16,
            Alphabet = "abc",
            CryptoVersion = 99,
            EncodingVersion = 1,
        };

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None));
    }

    [Fact]
    public async Task DecryptSecretAsync_UnsupportedCryptoVersion_Throws()
    {
        var secretData = new SecretDataV2
        {
            Cipher = Base58.Encode(new byte[100]),
            CryptoVersion = 1,
            Encoding = "base58",
        };

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.DecryptSecretAsync(secretData, _masterKey, CancellationToken.None));
    }
}
