using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Microsoft.Playwright;
using System.Text.Json;
using SysEncoding = System.Text.Encoding;

namespace ItchyPassword.Core.Tests.Crypto;

/// <summary>
/// Regression tests with hardcoded expected values for the JS crypto.js implementation.
/// Uses Playwright to execute every crypto operation in a real Chromium browser (SubtleCrypto).
/// If any test fails, it means crypto.js behavior has changed and existing vaults may be incompatible.
/// </summary>
public sealed class CryptoRegressionTests(PlaywrightFixture fixture) : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private IPage _page = null!;
    private PlaywrightCryptoService _crypto = null!;
    private VaultCryptoService _service = null!;
    private readonly byte[] _masterKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    public async Task InitializeAsync()
    {
        _page = await fixture.CreatePageWithCryptoAsync();
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

    // ── GeneratePasswordV1 regression ──────────────────────────────────

    [Fact]
    public async Task GeneratePasswordV1_KnownInputs_ProducesExpectedOutput()
    {
        byte[] publicPart = SysEncoding.UTF8.GetBytes("example.com");

        byte[] result = await _crypto.GeneratePasswordV1Async(_masterKey, publicPart, CancellationToken.None);

        string hex = Convert.ToHexString(result);
        Assert.Equal(
            "58BBFBA71ED50E775B098EA773673C62F7F885857F488A80BA16407B829D5F4B19B00FA07E48E0DE06273A549EB000AED9C4BD96B47A1DC9B65F28E011910125",
            hex);
    }

    [Fact]
    public async Task GeneratePasswordV1_OutputIs64Bytes()
    {
        byte[] publicPart = SysEncoding.UTF8.GetBytes("example.com");

        byte[] result = await _crypto.GeneratePasswordV1Async(_masterKey, publicPart, CancellationToken.None);

        // HMAC-SHA512 always produces 64 bytes.
        Assert.Equal(64, result.Length);
    }

    // ── GeneratePasswordV2 regression ──────────────────────────────────

    [Fact]
    public async Task GeneratePasswordV2_KnownInputs_ProducesExpectedOutput()
    {
        byte[] publicPart = SysEncoding.UTF8.GetBytes("example.com");

        byte[] result = await _crypto.GeneratePasswordV2Async(_masterKey, publicPart, "Password", CancellationToken.None);

        string hex = Convert.ToHexString(result);
        Assert.Equal(
            "9F39D8AC410BB80C340A757A70BC9BF9ADBB145184D2D182B77FF551561BA1537C5326CC78CC9397321BDE79FD72CC274EBAE2D33BE92107ED49C088FDBC8756",
            hex);
    }

    [Fact]
    public async Task GeneratePasswordV1_DiffersFromV2_SameInputs()
    {
        byte[] publicPart = SysEncoding.UTF8.GetBytes("example.com");

        byte[] v1 = await _crypto.GeneratePasswordV1Async(_masterKey, publicPart, CancellationToken.None);
        byte[] v2 = await _crypto.GeneratePasswordV2Async(_masterKey, publicPart, "Password", CancellationToken.None);

        Assert.NotEqual(Convert.ToHexString(v1), Convert.ToHexString(v2));
    }

    // ── HMAC-SHA512 regression ─────────────────────────────────────────

    [Fact]
    public async Task ComputeHmacSha512_KnownInputs_ProducesExpectedOutput()
    {
        byte[] data = SysEncoding.UTF8.GetBytes("test data");
        byte[] key = SysEncoding.UTF8.GetBytes("test key material!!");

        byte[] result = await _crypto.ComputeHmacSha512Async(data, key, CancellationToken.None);

        Assert.Equal(
            "5FB38FF909F70A6DB693B443143BCD4BB4DD900E9E434C3063DA3EA5913D6A4471DB5F12C9FFBB52A37BC3143D229957FFDAE1BC95F679C529C62E084102FC96",
            Convert.ToHexString(result));
    }

    [Fact]
    public async Task ComputeHmacSha512_Always64Bytes()
    {
        byte[] data = SysEncoding.UTF8.GetBytes("any data");
        byte[] key = SysEncoding.UTF8.GetBytes("any key");

        byte[] result = await _crypto.ComputeHmacSha512Async(data, key, CancellationToken.None);

        Assert.Equal(64, result.Length);
    }

    // ── Static key regression (V1 crypto + V1 encoding) ───────────────

    [Fact]
    public async Task GenerateStaticKey_V1_KnownInputs_ProducesExpectedOutput()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 16,
            Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789",
            CryptoVersion = 1,
            EncodingVersion = 1,
        };

        string result = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal("2l12si7trrxyushw", result);
    }

    // ── Static key regression (V2 crypto + V2 encoding) ───────────────

    [Fact]
    public async Task GenerateStaticKey_V2_UppercaseOnly_ProducesExpectedOutput()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 20,
            Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CryptoVersion = StaticKeyDataConstants.LatestCryptoVersion,
            EncodingVersion = StaticKeyDataConstants.LatestEncodingVersion,
        };

        string result = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal("MSJHHZIGGNDEKISOXNHK", result);
    }

    [Fact]
    public async Task GenerateStaticKey_V2_FullAlphabet_ProducesExpectedOutput()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "mybank.com",
            Length = 32,
            Alphabet = StaticKeyDataConstants.DefaultAlphabet,
            CryptoVersion = StaticKeyDataConstants.LatestCryptoVersion,
            EncodingVersion = StaticKeyDataConstants.LatestEncodingVersion,
        };

        string result = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal("#g~OkwHcX8gJ}VB0bXzHdStS?=iX^XYB", result);
    }

    [Fact]
    public async Task GenerateStaticKey_V2_DigitsOnly_ProducesExpectedOutput()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "pin.example.com",
            Length = 6,
            Alphabet = "0123456789",
            CryptoVersion = StaticKeyDataConstants.LatestCryptoVersion,
            EncodingVersion = StaticKeyDataConstants.LatestEncodingVersion,
        };

        string result = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal("101170", result);
    }

    // ── V1 vs V2 produce different output for same public part ────────

    [Fact]
    public async Task GenerateStaticKey_V1VsV2_DifferentResults_SamePublicPart()
    {
        var v1 = new StaticKeyDataV2
        {
            PublicPart = "shared.example.com",
            Length = 20,
            Alphabet = "abcdefghijklmnopqrstuvwxyz",
            CryptoVersion = 1,
            EncodingVersion = 1,
        };
        var v2 = new StaticKeyDataV2
        {
            PublicPart = "shared.example.com",
            Length = 20,
            Alphabet = "abcdefghijklmnopqrstuvwxyz",
            CryptoVersion = StaticKeyDataConstants.LatestCryptoVersion,
            EncodingVersion = StaticKeyDataConstants.LatestEncodingVersion,
        };

        string result1 = await _service.GenerateStaticKeyAsync(v1, _masterKey, CancellationToken.None);
        string result2 = await _service.GenerateStaticKeyAsync(v2, _masterKey, CancellationToken.None);

        Assert.Equal("bqddohanwlwbplagourc", result1);
        Assert.Equal("smecklrxdhcjgfftirgy", result2);
        Assert.NotEqual(result1, result2);
    }

    // ── Different master key produces different output ─────────────────

    [Fact]
    public async Task GenerateStaticKey_DifferentMasterKey_ProducesDifferentOutput()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 16,
            Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789",
            CryptoVersion = 1,
            EncodingVersion = 1,
        };

        byte[] altKey = [16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

        string result1 = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);
        string result2 = await _service.GenerateStaticKeyAsync(data, altKey, CancellationToken.None);

        Assert.Equal("2l12si7trrxyushw", result1);
        Assert.Equal("97mbeup410vqeae1", result2);
        Assert.NotEqual(result1, result2);
    }

    // ── Encrypt/decrypt round-trip via browser crypto ──────────────────

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_PreservesPlaintext()
    {
        string plaintext = "Super$ecretP@ssw0rd!";

        SecretDataV2 encrypted = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);
        string decrypted = await _service.DecryptSecretAsync(encrypted, _masterKey, CancellationToken.None);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_UnicodeContent()
    {
        string plaintext = "Пароль: 日本語テスト 🔐";

        SecretDataV2 encrypted = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);
        string decrypted = await _service.DecryptSecretAsync(encrypted, _masterKey, CancellationToken.None);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_LargePayload()
    {
        string plaintext = new('A', 10_000);

        SecretDataV2 encrypted = await _service.EncryptSecretAsync(plaintext, _masterKey, "base58", CancellationToken.None);
        string decrypted = await _service.DecryptSecretAsync(encrypted, _masterKey, CancellationToken.None);

        Assert.Equal(plaintext, decrypted);
    }

    // ── Vault signature regression ─────────────────────────────────────

    [Fact]
    public async Task VaultSignature_KnownVault_ProducesExpectedSignature()
    {
        var fixedId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var fixedDate = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var vault = new VaultV2
        {
            Version = 2,
            Items =
            [
                new VaultItemV2
                {
                    Id = fixedId,
                    Name = "TestService",
                    Type = VaultItemTypeV2.StaticKey,
                    CreatedAt = fixedDate,
                    LastModified = fixedDate,
                }
            ]
        };

        string signedJson = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        using JsonDocument doc = JsonDocument.Parse(signedJson);
        string signature = doc.RootElement.GetProperty("signature").GetString()!;

        Assert.Equal(
            "HCaRFJwkPyKLC68DKyTxWUDpWu4gXVLAd8oWkRDC66AkGzzEJc6RmoJFR7AqHSGhb4QzgNrQxNzjqMfBkPEe6Yb",
            signature);
    }

    // ── Encryption metadata ────────────────────────────────────────────

    [Fact]
    public async Task EncryptSecret_AlwaysUsesLatestVersionAndBase58()
    {
        SecretDataV2 result = await _service.EncryptSecretAsync("test", _masterKey, "base58", CancellationToken.None);

        Assert.Equal(SecretDataConstants.LatestCryptoVersion, result.CryptoVersion);
        Assert.Equal("base58", result.Encoding);
    }

    // ── Unsupported encoding version (calls crypto first, then throws) ─

    [Fact]
    public async Task GenerateStaticKey_UnsupportedEncodingVersion_Throws()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "test.com",
            Length = 16,
            Alphabet = "abc",
            CryptoVersion = 1,
            EncodingVersion = 99,
        };

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None));
    }

    // ── Static key length truncation ───────────────────────────────────

    [Fact]
    public async Task GenerateStaticKey_TruncatesToRequestedLength()
    {
        var data = new StaticKeyDataV2
        {
            PublicPart = "example.com",
            Length = 8,
            Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789",
            CryptoVersion = 1,
            EncodingVersion = 1,
        };

        string result = await _service.GenerateStaticKeyAsync(data, _masterKey, CancellationToken.None);

        Assert.Equal(8, result.Length);
        // The first 8 chars should match the full 16-char output truncated.
        Assert.Equal("2l12si7t", result);
    }

    // ── Encoding fallback behavior ─────────────────────────────────────

    [Fact]
    public async Task EncryptDecrypt_Base64Encoding_RoundTrips()
    {
        string plaintext = "base64 test";

        SecretDataV2 encrypted = await _service.EncryptSecretAsync(plaintext, _masterKey, "base64", CancellationToken.None);
        Assert.Equal("base64", encrypted.Encoding);

        string decrypted = await _service.DecryptSecretAsync(encrypted, _masterKey, CancellationToken.None);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task EncryptDecrypt_UnknownEncoding_FallsBackToBase58()
    {
        string plaintext = "fallback test";

        SecretDataV2 encrypted = await _service.EncryptSecretAsync(plaintext, _masterKey, "unknown_encoding", CancellationToken.None);

        // Unknown encoding should fall back to base58 on both encode and decode.
        string decrypted = await _service.DecryptSecretAsync(encrypted, _masterKey, CancellationToken.None);
        Assert.Equal(plaintext, decrypted);
    }
}
