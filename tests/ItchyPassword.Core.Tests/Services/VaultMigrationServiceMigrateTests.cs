using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.Security.Cryptography;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for the full <see cref="VaultMigrationService.MigrateAsync"/> pipeline
/// (structural migration + legacy password/cipher content migration).
///
/// These use a faithful fake <see cref="IVaultCryptoService"/> that models the key
/// invariant of AES-GCM: decryption only succeeds when the cipher is decrypted with the
/// exact same crypto-version and encoding it was produced with. That lets us assert on
/// migration LOGIC (which items get converted/re-encrypted, and that ciphers stay
/// decryptable end-to-end) without needing a real browser crypto backend.
/// </summary>
public sealed class VaultMigrationServiceMigrateTests
{
    private static readonly byte[] _masterKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    /// <summary>
    /// Fake crypto that encodes the (encoding, version, plaintext) triple into the cipher string.
    /// Decryption throws when the metadata it is handed does not match, exactly like a real
    /// AEAD would fail authentication when decoded/decrypted with the wrong parameters.
    /// </summary>
    private sealed class FakeVaultCryptoService : IVaultCryptoService
    {
        public List<(string Plaintext, string Encoding)> Encrypts { get; } = [];
        public List<StaticKeyDataV2> StaticKeyGenerations { get; } = [];

        public static string MakeLegacyCipher(string plaintext, string encoding, int version)
        {
            return $"PT|{encoding}|{version}|{plaintext}";
        }

        public Task<string> DecryptSecretAsync(SecretDataV2 secretData, byte[] masterKey, CancellationToken cancellationToken)
        {
            string[] parts = secretData.Cipher.Split('|');
            if (parts.Length < 4 || parts[0] != "PT")
            {
                throw new InvalidOperationException($"Not a recognizable fake cipher: '{secretData.Cipher}'.");
            }

            string encoding = parts[1];
            int version = int.Parse(parts[2]);
            string plaintext = string.Join('|', parts[3..]);

            if (encoding != secretData.Encoding || version != secretData.CryptoVersion)
            {
                throw new CryptographicException(
                    $"Simulated GCM failure: cipher was '{encoding}'/v{version} but decrypt was asked for '{secretData.Encoding}'/v{secretData.CryptoVersion}.");
            }

            return Task.FromResult(plaintext);
        }

        public Task<SecretDataV2> EncryptSecretAsync(string plaintext, byte[] masterKey, string encoding, CancellationToken cancellationToken)
        {
            Encrypts.Add((plaintext, encoding));
            return Task.FromResult(new SecretDataV2
            {
                Cipher = MakeLegacyCipher(plaintext, encoding, SecretDataConstants.LatestCryptoVersion),
                CryptoVersion = SecretDataConstants.LatestCryptoVersion,
                Encoding = encoding,
            });
        }

        public Task<string> GenerateStaticKeyAsync(StaticKeyDataV2 data, byte[] masterKey, CancellationToken cancellationToken)
        {
            StaticKeyGenerations.Add(data);
            return Task.FromResult($"GEN({data.PublicPart}|v{data.CryptoVersion}|e{data.EncodingVersion}|L{data.Length})");
        }
    }

    private static (VaultMigrationService Service, FakeVaultCryptoService Crypto, ErrorLogService Errors) CreateService()
    {
        var crypto = new FakeVaultCryptoService();
        var errors = new ErrorLogService();
        var service = new VaultMigrationService(crypto, errors);
        return (service, crypto, errors);
    }

    private static VaultItemV2 Single(VaultV2 vault, string name)
    {
        return vault.Items.Single(i => i.Name == name);
    }

    // ── Structural: a single password recipe ───────────────────────────

    [Fact]
    public async Task Migrate_SinglePasswordRecipe_BecomesStaticKeyItem()
    {
        string v1 = """
        {
            "GitHub": {
                "password": {
                    "public": "github.com/user",
                    "version": 1,
                    "length": 32,
                    "alphabet": "abcdef",
                    "datetime": "2023-01-02T03:04:05+00:00"
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal("GitHub", item.Name);
        Assert.Equal(VaultItemTypeV2.StaticKey, item.Type);
        Assert.NotNull(item.StaticKeyData);
        Assert.Equal("github.com/user", item.StaticKeyData!.Value.PublicPart);
        Assert.Equal(1, item.StaticKeyData.Value.CryptoVersion);
        Assert.Equal(32, item.StaticKeyData.Value.Length);
        Assert.Equal("abcdef", item.StaticKeyData.Value.Alphabet);
        Assert.Equal(new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.Zero), item.CreatedAt);
    }

    [Fact]
    public async Task Migrate_PasswordRecipe_UsesDefaultsForMissingFields()
    {
        string v1 = """
        {
            "Svc": {
                "password": {
                    "public": "example.com"
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(VaultItemTypeV2.StaticKey, item.Type);
        Assert.Equal(StaticKeyDataConstants.LatestCryptoVersion, item.StaticKeyData!.Value.CryptoVersion);
        Assert.Equal(StaticKeyDataConstants.DefaultLength, item.StaticKeyData.Value.Length);
        Assert.Equal(StaticKeyDataConstants.DefaultAlphabet, item.StaticKeyData.Value.Alphabet);
    }

    [Fact]
    public async Task Migrate_PasswordWithTooShortPublicPart_IsNotTreatedAsPassword()
    {
        // "public" shorter than 4 chars => the node is not a valid password object.
        string v1 = """
        {
            "Svc": {
                "password": {
                    "public": "ab"
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        Assert.Empty(vault.Items);
    }

    // ── Structural: ciphers ────────────────────────────────────────────

    [Fact]
    public async Task Migrate_SingleCipher_BecomesSecretItem()
    {
        string cipher = FakeVaultCryptoService.MakeLegacyCipher("my-secret-value", "base58", SecretDataConstants.LatestCryptoVersion);
        string v1 = $$"""
        {
            "Notes": {
                "ciphers": {
                    "recovery": {
                        "value": "{{cipher}}",
                        "version": 3,
                        "encoding": "base58",
                        "datetime": "2022-06-07T08:09:10+00:00"
                    }
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal("Notes / recovery", item.Name);
        Assert.Equal(VaultItemTypeV2.Secret, item.Type);
        Assert.Equal(new DateTimeOffset(2022, 6, 7, 8, 9, 10, TimeSpan.Zero), item.CreatedAt);
    }

    // ── End-to-end integrity: legacy ciphers stay decryptable ──────────

    [Theory]
    [InlineData("base58", 3)]
    [InlineData("base62", 3)]
    [InlineData("base62", 2)]
    [InlineData("base58", 2)]
    [InlineData("base64", 3)]
    public async Task Migrate_LegacyCipher_StaysDecryptableAndIsUpgradedToLatest(string encoding, int version)
    {
        const string plaintext = "correct horse battery staple";
        string cipher = FakeVaultCryptoService.MakeLegacyCipher(plaintext, encoding, version);
        string v1 = $$"""
        {
            "Svc": {
                "ciphers": {
                    "note": {
                        "value": "{{cipher}}",
                        "version": {{version}},
                        "encoding": "{{encoding}}"
                    }
                }
            }
        }
        """;
        (VaultMigrationService svc, FakeVaultCryptoService crypto, ErrorLogService errors) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(VaultItemTypeV2.Secret, item.Type);
        Assert.NotNull(item.SecretData);

        // After migration every cipher must be at the latest crypto version and encoding...
        Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
        Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);

        // ...and must still decrypt to the original plaintext.
        string decrypted = await crypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
        Assert.Equal(plaintext, decrypted);
        Assert.Equal(0, errors.Count);
    }

    [Fact]
    public async Task Migrate_CipherAlreadyLatest_IsLeftUntouched()
    {
        string cipher = FakeVaultCryptoService.MakeLegacyCipher("v", SecretDataConstants.LatestEncoding, SecretDataConstants.LatestCryptoVersion);
        string v1 = $$"""
        {
            "Svc": { "ciphers": { "n": { "value": "{{cipher}}", "version": 3, "encoding": "base58" } } }
        }
        """;
        (VaultMigrationService svc, FakeVaultCryptoService crypto, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        // No re-encryption should have happened (already latest).
        Assert.Empty(crypto.Encrypts);
        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(cipher, item.SecretData!.Value.Cipher);
    }

    // ── customKeys => metadata ─────────────────────────────────────────

    [Fact]
    public async Task Migrate_CipherWithCustomKeys_MapsToMetadata()
    {
        string cipher = FakeVaultCryptoService.MakeLegacyCipher("s", "base58", 3);
        string v1 = $$"""
        {
            "Svc": {
                "ciphers": {
                    "n": {
                        "value": "{{cipher}}",
                        "version": 3,
                        "encoding": "base58",
                        "customKeys": { "username": "alice", "url": "https://x" }
                    }
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.NotNull(item.Metadata);
        Assert.Contains(item.Metadata!, m => m.Key == "username" && m.Value == "alice");
        Assert.Contains(item.Metadata!, m => m.Key == "url" && m.Value == "https://x");
    }

    // ── Nested folders and path building ───────────────────────────────

    [Fact]
    public async Task Migrate_NestedFolders_BuildSlashSeparatedPaths()
    {
        string cipher = FakeVaultCryptoService.MakeLegacyCipher("s", "base58", 3);
        string v1 = $$"""
        {
            "Work": {
                "Email": {
                    "password": { "public": "mail.example.com" },
                    "ciphers": { "recovery": { "value": "{{cipher}}", "version": 3, "encoding": "base58" } }
                }
            }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        Assert.Equal(VaultItemTypeV2.StaticKey, Single(vault, "Work / Email").Type);
        Assert.Equal(VaultItemTypeV2.Secret, Single(vault, "Work / Email / recovery").Type);
    }

    // ── Legacy password recipe heuristic conversion ────────────────────

    [Fact]
    public async Task Migrate_ReadablePublicPart_StaysStaticKey()
    {
        // A normal deterministic-password recipe: readable public part with non-base62 chars.
        string v1 = """
        {
            "GitHub": { "password": { "public": "github.com/user", "version": 2 } }
        }
        """;
        (VaultMigrationService svc, FakeVaultCryptoService crypto, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        Assert.Equal(VaultItemTypeV2.StaticKey, Assert.Single(vault.Items).Type);
        Assert.Empty(crypto.Encrypts);
    }

    [Fact]
    public async Task Migrate_RandomLongBase62PublicPart_IsConvertedToSecret()
    {
        // A generated (random) public part: long and strictly base62 => stored password, converted to secret.
        string longPublic = new string('a', 30);
        string v1 = $$"""
        {
            "Rnd": { "password": { "public": "{{longPublic}}", "version": 2 } }
        }
        """;
        (VaultMigrationService svc, FakeVaultCryptoService crypto, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(VaultItemTypeV2.Secret, item.Type);
        Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
        Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);

        // The stored secret must decrypt to the deterministic password V1 would have generated.
        string decrypted = await crypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
        Assert.StartsWith("GEN(" + longPublic, decrypted);
    }

    // ── Multiple items and integrity across a realistic vault ──────────

    [Fact]
    public async Task Migrate_RealisticMixedVault_MigratesEveryCipherWithoutErrors()
    {
        string b62 = FakeVaultCryptoService.MakeLegacyCipher("secret-A", "base62", 3);
        string b58v2 = FakeVaultCryptoService.MakeLegacyCipher("secret-B", "base58", 2);
        string b58v3 = FakeVaultCryptoService.MakeLegacyCipher("secret-C", "base58", 3);
        string v1 = $$"""
        {
            "Bank": {
                "password": { "public": "bank.example.com", "version": 2, "length": 20 },
                "ciphers": {
                    "pin":  { "value": "{{b62}}",   "version": 3, "encoding": "base62" },
                    "note": { "value": "{{b58v2}}", "version": 2, "encoding": "base58" }
                }
            },
            "Mail": {
                "ciphers": {
                    "recovery": { "value": "{{b58v3}}", "version": 3, "encoding": "base58" }
                }
            }
        }
        """;
        (VaultMigrationService svc, FakeVaultCryptoService crypto, ErrorLogService errors) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        Assert.Equal(0, errors.Count);
        Assert.Equal(4, vault.Items.Count);

        async Task AssertDecrypts(string name, string expected)
        {
            VaultItemV2 item = Single(vault, name);
            Assert.Equal(VaultItemTypeV2.Secret, item.Type);
            Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
            Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);
            string decrypted = await crypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
            Assert.Equal(expected, decrypted);
        }

        await AssertDecrypts("Bank / pin", "secret-A");
        await AssertDecrypts("Bank / note", "secret-B");
        await AssertDecrypts("Mail / recovery", "secret-C");
        Assert.Equal(VaultItemTypeV2.StaticKey, Single(vault, "Bank").Type);
    }

    // ── Progress reporting ─────────────────────────────────────────────

    [Fact]
    public async Task Migrate_ReportsProgress()
    {
        string cipher = FakeVaultCryptoService.MakeLegacyCipher("s", "base62", 3);
        string v1 = $$"""
        {
            "Svc": { "ciphers": { "n": { "value": "{{cipher}}", "version": 3, "encoding": "base62" } } }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();
        var reported = new List<double>();
        var progress = new Progress<double>(reported.Add);

        await svc.MigrateAsync(v1, _masterKey, progress, CancellationToken.None);

        // Progress is async; give the synchronization context a chance to flush.
        await Task.Delay(50);
        Assert.Contains(100.0, reported);
    }

    // ── Empty / edge vaults ────────────────────────────────────────────

    [Fact]
    public async Task Migrate_EmptyVault_ReturnsEmptyV2()
    {
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync("{}", _masterKey, null, CancellationToken.None);

        Assert.Equal(2, vault.Version);
        Assert.Empty(vault.Items);
    }

    // ── Robustness: a single malformed entry must not lose the whole vault ─

    [Fact]
    public async Task Migrate_CipherWithStringTypedVersion_DoesNotDropOtherItems()
    {
        // A hand-edited / legacy-exported vault where "version" is a JSON string instead of a number.
        // This used to make ExtractValue throw, aborting the whole structural migration and silently
        // dropping every entry that came after it.
        string good = FakeVaultCryptoService.MakeLegacyCipher("keep-me", "base58", 3);
        string weird = FakeVaultCryptoService.MakeLegacyCipher("also-keep-me", "base58", 3);
        string v1 = $$"""
        {
            "Aaa": { "ciphers": { "weird": { "value": "{{weird}}", "version": "3", "encoding": "base58" } } },
            "Bbb": { "ciphers": { "good":  { "value": "{{good}}",  "version": 3,   "encoding": "base58" } } }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        // Both services must survive migration.
        Assert.Equal(2, vault.Items.Count);
        Assert.Contains(vault.Items, i => i.Name == "Aaa / weird");
        Assert.Contains(vault.Items, i => i.Name == "Bbb / good");
    }

    [Fact]
    public async Task Migrate_PasswordWithStringTypedLength_CoercesInsteadOfLosingIt()
    {
        // "length" stored as a JSON string must still be honoured, not silently reset to the default.
        string v1 = """
        {
            "Svc": { "password": { "public": "example.com", "version": 1, "length": "24", "alphabet": "abc" } }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(24, item.StaticKeyData!.Value.Length);
    }

    [Fact]
    public async Task Migrate_UnparseableDatetime_FallsBackToNowNotMinValue()
    {
        string v1 = """
        {
            "Svc": { "password": { "public": "example.com", "datetime": "not-a-date" } }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        // The fallback must be "now", never DateTimeOffset.MinValue.
        Assert.True(item.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Migrate_IsoDatetime_ParsedRegardlessOfMachineCulture()
    {
        string v1 = """
        {
            "Svc": { "ciphers": { "n": { "value": "PT|base58|3|s", "version": 3, "encoding": "base58",
                                          "datetime": "2021-03-04T05:06:07.089Z" } } }
        }
        """;
        (VaultMigrationService svc, _, _) = CreateService();

        VaultV2 vault = await svc.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(new DateTimeOffset(2021, 3, 4, 5, 6, 7, 89, TimeSpan.Zero), item.CreatedAt);
    }
}
