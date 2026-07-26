using ItchyPassword.Core.Constants;
using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Microsoft.Playwright;
using System.Numerics;
using System.Text.Json.Nodes;
using SysEncoding = System.Text.Encoding;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// End-to-end migration tests that build genuine Legacy V1 ciphers with the real browser
/// crypto (crypto.js) and the exact V1 encodings, then run them through the full
/// <see cref="VaultMigrationService.MigrateAsync"/> pipeline and verify the migrated ciphers
/// still decrypt to the original plaintext.
///
/// This is the faithful reproduction of "imported a V1 vault, some ciphers came out corrupted".
/// </summary>
public sealed class VaultMigrationEndToEndTests(PlaywrightFixture fixture) : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private IPage _page = null!;
    private PlaywrightCryptoService _crypto = null!;
    private VaultCryptoService _vaultCrypto = null!;
    private VaultMigrationService _migration = null!;
    private readonly byte[] _masterKey = SysEncoding.UTF8.GetBytes("this-is-a-master-key-123456");

    public async Task InitializeAsync()
    {
        _page = await fixture.CreatePageWithCryptoAsync();

        // Provide a V1-compatible encryptV2 (100k iterations) so we can build genuine v2 ciphers.
        await _page.EvaluateAsync(@"() => {
            window.ItchyPassword.Crypto.encryptV2 = async function(input, password) {
                const iterations = 100000;
                const nonce = window.crypto.getRandomValues(new Uint8Array(12));
                const salt = window.crypto.getRandomValues(new Uint8Array(16));
                const baseKey = await window.crypto.subtle.importKey('raw', password, { name: 'PBKDF2' }, false, ['deriveKey']);
                const derivedKey = await window.crypto.subtle.deriveKey(
                    { name: 'PBKDF2', hash: 'SHA-512', iterations, salt }, baseKey,
                    { name: 'AES-GCM', length: 256 }, false, ['encrypt']);
                const encrypted = await window.crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, derivedKey, input);
                const output = new Uint8Array(12 + 16 + encrypted.byteLength);
                output.set(nonce, 0); output.set(salt, 12); output.set(new Uint8Array(encrypted), 28);
                return output;
            };
        }");

        _crypto = new PlaywrightCryptoService(_page);
        _vaultCrypto = new VaultCryptoService(_crypto);
        _migration = new VaultMigrationService(_vaultCrypto, new ErrorLogService());
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }
    }

    private async Task<byte[]> EncryptRawAsync(string plaintext, int version)
    {
        string inputB64 = Convert.ToBase64String(SysEncoding.UTF8.GetBytes(plaintext));
        string passwordB64 = Convert.ToBase64String(_masterKey);
        string fn = version == 2 ? "encryptV2" : "encryptV3";

        string result = await _page.EvaluateAsync<string>($@"
            async () => {{
                const input = __fromB64('{inputB64}');
                const password = __fromB64('{passwordB64}');
                const encrypted = await window.ItchyPassword.Crypto.{fn}(input, password);
                return __toB64(encrypted);
            }}");

        return Convert.FromBase64String(result);
    }

    // ── V1 encodings (verified byte-identical to the real crypto/arrayUtils) ─

    private static readonly string _b62Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string _b58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    private static string EncodeLegacy(byte[] data, string encoding)
    {
        return encoding switch
        {
            "base58" => JsBase58Encode(data),
            "base62" => JsBase62Encode(data),
            "base64" => Convert.ToBase64String(data),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding)),
        };
    }

    // Mirrors arrayUtils.toCustomBase (2-byte LE length header, little-endian bigint, base62).
    private static string JsBase62Encode(byte[] data)
    {
        var headered = new byte[2 + data.Length];
        headered[0] = (byte)(data.Length % 256);
        headered[1] = (byte)(data.Length / 256 % 256);
        Array.Copy(data, 0, headered, 2, data.Length);

        var number = new BigInteger(headered, isUnsigned: true, isBigEndian: false);
        BigInteger b = _b62Alphabet.Length;
        var sb = new System.Text.StringBuilder();
        while (number > 0)
        {
            number = BigInteger.DivRem(number, b, out BigInteger rem);
            sb.Append(_b62Alphabet[(int)rem]);
        }
        return sb.ToString();
    }

    // Mirrors arrayUtils.toCustomBaseFast (Bitcoin-style base58).
    private static string JsBase58Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var digits = new List<int> { 0 };
        foreach (byte by in data)
        {
            for (int j = 0; j < digits.Count; j++)
            {
                digits[j] <<= 8;
            }
            digits[0] += by;
            int carry = 0;
            for (int j = 0; j < digits.Count; j++)
            {
                digits[j] += carry;
                carry = digits[j] / 58;
                digits[j] %= 58;
            }
            while (carry > 0)
            {
                digits.Add(carry % 58);
                carry /= 58;
            }
        }
        for (int i = 0; i < data.Length - 1 && data[i] == 0; i++)
        {
            digits.Add(0);
        }
        var sb = new System.Text.StringBuilder();
        for (int i = digits.Count - 1; i >= 0; i--)
        {
            sb.Append(_b58Alphabet[digits[i]]);
        }
        return sb.ToString();
    }

    [Theory]
    [InlineData("base58", 3)]
    [InlineData("base62", 3)]
    [InlineData("base64", 3)]
    [InlineData("base58", 2)]
    [InlineData("base62", 2)]
    [InlineData("base64", 2)]
    public async Task Migrate_RealLegacyCipher_StaysDecryptable(string encoding, int version)
    {
        const string plaintext = "Super$ecret! 日本語 🔐 correct-horse";
        byte[] raw = await EncryptRawAsync(plaintext, version);
        string legacyValue = EncodeLegacy(raw, encoding);

        string v1 = $$"""
        {
            "MyService": {
                "ciphers": {
                    "note": { "value": "{{legacyValue}}", "version": {{version}}, "encoding": "{{encoding}}" }
                }
            }
        }
        """;

        VaultV2 vault = await _migration.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(VaultItemTypeV2.Secret, item.Type);
        Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
        Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);

        string decrypted = await _vaultCrypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(200)]
    [InlineData(1000)]
    public async Task Migrate_RealLegacyCipher_VariousPlaintextLengths_StayDecryptable(int length)
    {
        string plaintext = length == 0 ? "x" : new string('A', length);
        byte[] raw = await EncryptRawAsync(plaintext, 3);

        foreach (string encoding in new[] { "base58", "base62", "base64" })
        {
            string legacyValue = EncodeLegacy(raw, encoding);
            string v1 = $$"""
            { "S": { "ciphers": { "n": { "value": "{{legacyValue}}", "version": 3, "encoding": "{{encoding}}" } } } }
            """;

            VaultV2 vault = await _migration.MigrateAsync(v1, _masterKey, null, CancellationToken.None);
            VaultItemV2 item = Assert.Single(vault.Items);
            string decrypted = await _vaultCrypto.DecryptSecretAsync(item.SecretData!.Value, _masterKey, CancellationToken.None);
            Assert.Equal(plaintext, decrypted);
        }
    }

    [Fact]
    public async Task Migrate_ThenSaveAndReload_CiphersStillDecrypt()
    {
        // Reproduces the full user flow: import V1 -> migrate -> persist (serialize + sign) ->
        // reload -> decrypt. A corruption anywhere in that round-trip would surface here.
        const string plaintextPin = "1234-secret-pin";
        const string plaintextNote = "recovery codes: aaa bbb ccc";
        string pinValue = EncodeLegacy(await EncryptRawAsync(plaintextPin, 2), "base62");
        string noteValue = EncodeLegacy(await EncryptRawAsync(plaintextNote, 3), "base58");

        string v1 = $$"""
        {
            "Bank": {
                "password": { "public": "bank.example.com", "version": 2, "length": 20 },
                "ciphers": {
                    "pin":  { "value": "{{pinValue}}",  "version": 2, "encoding": "base62" },
                    "note": { "value": "{{noteValue}}", "version": 3, "encoding": "base58" }
                }
            }
        }
        """;

        VaultV2 migrated = await _migration.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        // Persist and reload exactly like the app does.
        string saved = await VaultDataService.SerializeAndSignAsync(migrated, _masterKey, _crypto, CancellationToken.None);
        VaultDeserializeResult reloaded = await VaultDataService.DeserializeAndVerifyAsync(saved, _masterKey, _crypto, CancellationToken.None);

        Assert.Equal(VaultSignatureStatus.Valid, reloaded.SignatureStatus);
        Assert.NotNull(reloaded.Vault);

        VaultItemV2 pin = reloaded.Vault!.Value.Items.Single(i => i.Name == "Bank / pin");
        VaultItemV2 note = reloaded.Vault.Value.Items.Single(i => i.Name == "Bank / note");

        Assert.Equal(plaintextPin, await _vaultCrypto.DecryptSecretAsync(pin.SecretData!.Value, _masterKey, CancellationToken.None));
        Assert.Equal(plaintextNote, await _vaultCrypto.DecryptSecretAsync(note.SecretData!.Value, _masterKey, CancellationToken.None));
    }

    [Fact]
    public async Task Migrate_RealLegacyPasswordRecipe_GeneratesSameKeyAsV1()
    {
        // A short readable public part => stays a static key; regenerate and compare with V1 crypto.
        string v1 = """
        {
            "GitHub": {
                "password": { "public": "github.com/user", "version": 2, "length": 20, "alphabet": "abcdefghijklmnopqrstuvwxyz" }
            }
        }
        """;

        VaultV2 vault = await _migration.MigrateAsync(v1, _masterKey, null, CancellationToken.None);

        VaultItemV2 item = Assert.Single(vault.Items);
        Assert.Equal(VaultItemTypeV2.StaticKey, item.Type);

        // Reference: what V1 produced = PasswordGeneratorV2 (400k) + toCustomBaseOneWay, truncated.
        byte[] keyBytes = await _crypto.GeneratePasswordV2Async(_masterKey, SysEncoding.UTF8.GetBytes("github.com/user"), "Password", CancellationToken.None);
        string expected = BaseN.EncodeOneWay(keyBytes, "abcdefghijklmnopqrstuvwxyz");
        if (expected.Length > 20)
        {
            expected = expected[..20];
        }

        string actual = await _vaultCrypto.GenerateStaticKeyAsync(item.StaticKeyData!.Value, _masterKey, CancellationToken.None);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Builds one V1 vault holding EVERY crypto/encoding/kind combination, migrates it once,
    /// and asserts that every migrated value is strictly identical to what V1 would have produced:
    /// each cipher decrypts to its original plaintext, and each password recipe yields exactly the
    /// password V1 would generate. Also checks types, paths, metadata, dates, and that every secret
    /// ends up at the latest crypto version and encoding.
    /// </summary>
    [Fact]
    public async Task Migrate_FullMatrixVault_EveryValueMatchesV1()
    {
        string[] encodings = ["base58", "base62", "base64"];
        int[] cipherVersions = [2, 3];

        // Expected cipher plaintexts, keyed by the migrated item name.
        var expectedCipherPlaintext = new Dictionary<string, string>();

        var ciphers = new JsonObject();
        foreach (string encoding in encodings)
        {
            foreach (int version in cipherVersions)
            {
                string key = $"c-{encoding}-v{version}";
                string plaintext = $"secret::{encoding}::v{version}::日本語🔐";
                byte[] raw = await EncryptRawAsync(plaintext, version);

                var detail = new JsonObject
                {
                    ["value"] = EncodeLegacy(raw, encoding),
                    ["version"] = version,
                    ["encoding"] = encoding,
                    ["datetime"] = "2020-01-02T03:04:05.000Z",
                };

                // Attach custom keys to exactly one cipher to exercise metadata mapping.
                if (encoding == "base58" && version == 3)
                {
                    detail["customKeys"] = new JsonObject { ["user"] = "bob", ["url"] = "https://x" };
                }

                ciphers[key] = detail;
                expectedCipherPlaintext[$"Vault / {key}"] = plaintext;
            }
        }

        // Password recipes with readable (non-base62) public parts stay static keys.
        (string Name, string Public, int Version, string Alphabet, int Length)[] statics =
        [
            ("StaticV1", "site-v1.example.com/user", 1, "abcdefghijklmnopqrstuvwxyz", 20),
            ("StaticV2", "site-v2.example.com/user", 2, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 32),
        ];

        // Password recipes with long, strictly-base62 public parts are converted to secrets.
        (string Name, string Public, int Version, string Alphabet, int Length)[] randoms =
        [
            ("RandomV1", "aB3cD4eF5gH6iJ7kL8mN9oP0qR1sT2uVwX", 1, StaticKeyDataConstants.DefaultAlphabet, 40),
            ("RandomV2", "Zx9Yw8Vu7Ts6Rq5Po4Nm3Lk2Ji1Hg0FeDcBa", 2, StaticKeyDataConstants.DefaultAlphabet, 50),
        ];

        var root = new JsonObject { ["Vault"] = new JsonObject { ["ciphers"] = ciphers } };
        foreach ((string name, string pub, int version, string alphabet, int length) in statics.Concat(randoms))
        {
            var password = new JsonObject
            {
                ["public"] = pub,
                ["version"] = version,
                ["length"] = length,
                ["alphabet"] = alphabet,
            };
            if (name == "StaticV2")
            {
                password["customKeys"] = new JsonObject { ["note"] = "primary" };
            }
            root[name] = new JsonObject { ["password"] = password };
        }

        // ── Migrate the whole matrix in one pass ───────────────────────
        VaultV2 vault = await _migration.MigrateAsync(root.ToJsonString(), _masterKey, null, CancellationToken.None);

        // 6 ciphers + 2 static keys + 2 converted randoms.
        Assert.Equal(10, vault.Items.Count);

        // Reference V1 password generation (crypto in the browser, encoding validated by regression).
        async Task<string> ExpectedV1Password(string pub, int version, string alphabet, int length)
        {
            byte[] keyBytes = version == 1
                ? await _crypto.GeneratePasswordV1Async(_masterKey, SysEncoding.UTF8.GetBytes(pub), CancellationToken.None)
                : await _crypto.GeneratePasswordV2Async(_masterKey, SysEncoding.UTF8.GetBytes(pub), "Password", CancellationToken.None);

            string s = BaseN.EncodeOneWay(keyBytes, alphabet);
            return s.Length > length ? s[..length] : s;
        }

        // ── Every cipher: strictly equal decrypted value, upgraded to latest ──
        foreach ((string name, string expectedPlaintext) in expectedCipherPlaintext)
        {
            VaultItemV2 item = vault.Items.Single(i => i.Name == name);
            Assert.Equal(VaultItemTypeV2.Secret, item.Type);
            Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
            Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);
            Assert.Equal(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), item.CreatedAt);

            string decrypted = await _vaultCrypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
            Assert.Equal(expectedPlaintext, decrypted);
        }

        // Metadata preserved from customKeys.
        VaultItemV2 cipherWithMeta = vault.Items.Single(i => i.Name == "Vault / c-base58-v3");
        Assert.NotNull(cipherWithMeta.Metadata);
        Assert.Contains(cipherWithMeta.Metadata!, m => m.Key == "user" && m.Value == "bob");
        Assert.Contains(cipherWithMeta.Metadata!, m => m.Key == "url" && m.Value == "https://x");

        // ── Static-key recipes: strictly equal generated password ──────
        foreach ((string name, string pub, int version, string alphabet, int length) in statics)
        {
            VaultItemV2 item = vault.Items.Single(i => i.Name == name);
            Assert.Equal(VaultItemTypeV2.StaticKey, item.Type);

            string expected = await ExpectedV1Password(pub, version, alphabet, length);
            string actual = await _vaultCrypto.GenerateStaticKeyAsync(item.StaticKeyData!.Value, _masterKey, CancellationToken.None);
            Assert.Equal(expected, actual);
        }

        Assert.Contains(vault.Items.Single(i => i.Name == "StaticV2").Metadata!, m => m.Key == "note" && m.Value == "primary");

        // ── Converted random recipes: secret decrypts to the exact V1 password ──
        foreach ((string name, string pub, int version, string alphabet, int length) in randoms)
        {
            VaultItemV2 item = vault.Items.Single(i => i.Name == name);
            Assert.Equal(VaultItemTypeV2.Secret, item.Type);
            Assert.Equal(SecretDataConstants.LatestCryptoVersion, item.SecretData!.Value.CryptoVersion);
            Assert.Equal(SecretDataConstants.LatestEncoding, item.SecretData.Value.Encoding);

            string expected = await ExpectedV1Password(pub, version, alphabet, length);
            string decrypted = await _vaultCrypto.DecryptSecretAsync(item.SecretData.Value, _masterKey, CancellationToken.None);
            Assert.Equal(expected, decrypted);
        }
    }
}
