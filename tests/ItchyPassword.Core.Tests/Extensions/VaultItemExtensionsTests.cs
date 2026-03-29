using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Extensions;
using ItchyPassword.Core.Models;

namespace ItchyPassword.Core.Tests.Extensions;

/// <summary>
/// Tests for <see cref="VaultItemExtensions.IsLegacy"/>.
/// </summary>
public sealed class VaultItemExtensionsTests
{
    private static VaultItemV2 CreateStaticKeyItem(int cryptoVersion, int encodingVersion)
    {
        var item = new VaultItemV2
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Type = VaultItemTypeV2.StaticKey,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        item.SetData(new StaticKeyDataV2
        {
            PublicPart = "test.com",
            Alphabet = "abc",
            Length = 16,
            CryptoVersion = cryptoVersion,
            EncodingVersion = encodingVersion,
        });
        return item;
    }

    private static VaultItemV2 CreateSecretItem(int cryptoVersion, string encoding)
    {
        var item = new VaultItemV2
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Type = VaultItemTypeV2.Secret,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };
        item.SetData(new SecretDataV2
        {
            Cipher = "testcipher",
            CryptoVersion = cryptoVersion,
            Encoding = encoding,
        });
        return item;
    }

    // ── StaticKey items ────────────────────────────────────────────────

    [Fact]
    public void IsLegacy_StaticKey_LatestVersions_ReturnsFalse()
    {
        VaultItemV2 item = CreateStaticKeyItem(
            StaticKeyDataConstants.LatestCryptoVersion,
            StaticKeyDataConstants.LatestEncodingVersion);

        Assert.False(item.IsLegacy());
    }

    [Fact]
    public void IsLegacy_StaticKey_OldCryptoVersion_ReturnsTrue()
    {
        VaultItemV2 item = CreateStaticKeyItem(1, StaticKeyDataConstants.LatestEncodingVersion);

        Assert.True(item.IsLegacy());
    }

    [Fact]
    public void IsLegacy_StaticKey_OldEncodingVersion_ReturnsTrue()
    {
        VaultItemV2 item = CreateStaticKeyItem(StaticKeyDataConstants.LatestCryptoVersion, 1);

        Assert.True(item.IsLegacy());
    }

    [Fact]
    public void IsLegacy_StaticKey_BothOld_ReturnsTrue()
    {
        VaultItemV2 item = CreateStaticKeyItem(1, 1);

        Assert.True(item.IsLegacy());
    }

    // ── Secret items ───────────────────────────────────────────────────

    [Fact]
    public void IsLegacy_Secret_LatestVersionAndBase58_ReturnsFalse()
    {
        VaultItemV2 item = CreateSecretItem(SecretDataConstants.LatestCryptoVersion, "base58");

        Assert.False(item.IsLegacy());
    }

    [Fact]
    public void IsLegacy_Secret_OldCryptoVersion_ReturnsTrue()
    {
        VaultItemV2 item = CreateSecretItem(2, "base58");

        Assert.True(item.IsLegacy());
    }

    [Fact]
    public void IsLegacy_Secret_LegacyEncoding_ReturnsTrue()
    {
        VaultItemV2 item = CreateSecretItem(SecretDataConstants.LatestCryptoVersion, "base62");

        Assert.True(item.IsLegacy());
    }

    // ── Items without data ─────────────────────────────────────────────

    [Fact]
    public void IsLegacy_ItemWithNoData_ReturnsFalse()
    {
        var item = new VaultItemV2
        {
            Id = Guid.NewGuid(),
            Name = "Empty",
            Type = VaultItemTypeV2.StaticKey,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        };

        Assert.False(item.IsLegacy());
    }

    // ── SetData mutual exclusion ───────────────────────────────────────

    [Fact]
    public void SetData_SecretData_ClearsStaticKeyData()
    {
        VaultItemV2 item = CreateStaticKeyItem(1, 1);
        Assert.NotNull(item.StaticKeyData);

        item.SetData(new SecretDataV2
        {
            Cipher = "cipher",
            CryptoVersion = 3,
            Encoding = "base58",
        });

        Assert.NotNull(item.SecretData);
        Assert.Null(item.StaticKeyData);
    }

    [Fact]
    public void SetData_StaticKeyData_ClearsSecretData()
    {
        VaultItemV2 item = CreateSecretItem(3, "base58");
        Assert.NotNull(item.SecretData);

        item.SetData(new StaticKeyDataV2
        {
            PublicPart = "test",
            Alphabet = "abc",
            Length = 10,
            CryptoVersion = 2,
            EncodingVersion = 2,
        });

        Assert.NotNull(item.StaticKeyData);
        Assert.Null(item.SecretData);
    }
}
