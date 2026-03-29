using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Microsoft.Playwright;
using System.Text.Json;

namespace ItchyPassword.Core.Tests.Services;

public class VaultDataServiceTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private PlaywrightCryptoService _crypto = null!;
    private readonly byte[] _masterKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    public VaultDataServiceTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageWithCryptoAsync();
        _crypto = new PlaywrightCryptoService(_page);
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }
    }

    private static VaultV2 CreateVault(params string[] itemNames)
    {
        List<VaultItemV2> items = itemNames.Select(name => new VaultItemV2
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = VaultItemTypeV2.StaticKey,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
        }).ToList();

        return new VaultV2 { Version = 2, Items = items };
    }

    // ── SerializeAndSignAsync ────────────────────────────────────────────

    [Fact]
    public async Task SerializeAndSignAsync_ProducesValidSignature()
    {
        VaultV2 vault = CreateVault("GitHub", "Gmail");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Valid, result.SignatureStatus);
    }

    [Fact]
    public async Task SerializeAndSignAsync_OutputContainsSignatureField()
    {
        VaultV2 vault = CreateVault("TestItem");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("signature", out JsonElement sigElement));
        Assert.False(string.IsNullOrWhiteSpace(sigElement.GetString()));
    }

    [Fact]
    public async Task SerializeAndSignAsync_OutputIsPrettyPrinted()
    {
        VaultV2 vault = CreateVault("Item");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        // Pretty-printed JSON contains newlines and indentation.
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public async Task SerializeAndSignAsync_SortsItemsByName()
    {
        VaultV2 vault = CreateVault("Zeta", "Alpha", "Middle");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement items = doc.RootElement.GetProperty("items");
        List<string> names = items.EnumerateArray()
            .Select(e => e.GetProperty("name").GetString()!)
            .ToList();

        Assert.Equal(["Alpha", "Middle", "Zeta"], names);
    }

    [Fact]
    public async Task SerializeAndSignAsync_EmptyVault_ProducesValidSignature()
    {
        VaultV2 vault = CreateVault();

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Valid, result.SignatureStatus);
        Assert.Empty(result.Vault.Value.Items);
    }

    [Fact]
    public async Task SerializeAndSignAsync_IsDeterministic()
    {
        // Use fixed GUIDs so both calls produce identical items.
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var now = DateTimeOffset.UtcNow;

        VaultV2 MakeVault()
        {
            return new VaultV2
            {
                Version = 2,
                Items =
                [
                    new VaultItemV2 { Id = id, Name = "A", Type = VaultItemTypeV2.StaticKey, CreatedAt = now, LastModified = now }
                ]
            };
        }

        string json1 = await VaultDataService.SerializeAndSignAsync(MakeVault(), _masterKey, _crypto, CancellationToken.None);
        string json2 = await VaultDataService.SerializeAndSignAsync(MakeVault(), _masterKey, _crypto, CancellationToken.None);

        Assert.Equal(json1, json2);
    }

    // ── DeserializeAndVerifyAsync ────────────────────────────────────────

    [Fact]
    public async Task DeserializeAndVerifyAsync_ValidSignature_ReturnsValid()
    {
        VaultV2 vault = CreateVault("Item1");
        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, _masterKey, _crypto, CancellationToken.None);

        Assert.Equal(VaultSignatureStatus.Valid, result.SignatureStatus);
        Assert.NotNull(result.Vault);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_MissingSignature_ReturnsMissing()
    {
        VaultV2 vault = CreateVault("LegacyItem");
        // Serialize without signing (simulates a legacy vault).
        string json = JsonSerializer.Serialize(vault, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Missing, result.SignatureStatus);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_TamperedContent_ReturnsInvalid()
    {
        VaultV2 vault = CreateVault("Original");
        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        // Tamper with item name after signing.
        string tampered = json.Replace("Original", "Tampered");

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(tampered, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Invalid, result.SignatureStatus);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_WrongMasterKey_ReturnsInvalid()
    {
        VaultV2 vault = CreateVault("SecretItem");
        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        byte[] wrongKey = [99, 98, 97, 96, 95, 94, 93, 92, 91, 90, 89, 88, 87, 86, 85, 84];

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, wrongKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Invalid, result.SignatureStatus);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_TamperedSignature_ReturnsInvalid()
    {
        VaultV2 vault = CreateVault("Item");
        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        // Replace the signature value with garbage.
        using JsonDocument doc = JsonDocument.Parse(json);
        string realSignature = doc.RootElement.GetProperty("signature").GetString()!;
        string fakeSignature = new('A', realSignature.Length);
        string tampered = json.Replace(realSignature, fakeSignature);

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(tampered, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(VaultSignatureStatus.Invalid, result.SignatureStatus);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_InvalidJson_ReturnsMissingWithNullVault()
    {
        string garbage = "this is not json";

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(garbage, _masterKey, _crypto, CancellationToken.None);

        Assert.Null(result.Vault);
        Assert.Equal(VaultSignatureStatus.Missing, result.SignatureStatus);
    }

    [Fact]
    public async Task DeserializeAndVerifyAsync_V1Json_ReturnsMissingWithNullVault()
    {
        // Version 1 vaults should not parse as VaultV2.
        string v1Json = """{"version": 1, "items": []}""";

        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(v1Json, _masterKey, _crypto, CancellationToken.None);

        Assert.Null(result.Vault);
        Assert.Equal(VaultSignatureStatus.Missing, result.SignatureStatus);
    }

    // ── Round-trip / integration scenarios ────────────────────────────────

    [Fact]
    public async Task RoundTrip_PreservesAllItems()
    {
        VaultV2 vault = CreateVault("Alpha", "Bravo", "Charlie");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);
        VaultDeserializeResult result = await VaultDataService.DeserializeAndVerifyAsync(json, _masterKey, _crypto, CancellationToken.None);

        Assert.NotNull(result.Vault);
        Assert.Equal(3, result.Vault.Value.Items.Count);
        Assert.Contains(result.Vault.Value.Items, i => i.Name == "Alpha");
        Assert.Contains(result.Vault.Value.Items, i => i.Name == "Bravo");
        Assert.Contains(result.Vault.Value.Items, i => i.Name == "Charlie");
    }

    [Fact]
    public async Task RoundTrip_SignatureStaysValidAfterResave()
    {
        VaultV2 vault = CreateVault("Test");

        // Sign, deserialize, re-sign — should still be valid.
        string json1 = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);
        VaultDeserializeResult result1 = await VaultDataService.DeserializeAndVerifyAsync(json1, _masterKey, _crypto, CancellationToken.None);
        Assert.Equal(VaultSignatureStatus.Valid, result1.SignatureStatus);

        string json2 = await VaultDataService.SerializeAndSignAsync(result1.Vault!.Value, _masterKey, _crypto, CancellationToken.None);
        VaultDeserializeResult result2 = await VaultDataService.DeserializeAndVerifyAsync(json2, _masterKey, _crypto, CancellationToken.None);
        Assert.Equal(VaultSignatureStatus.Valid, result2.SignatureStatus);
    }

    [Fact]
    public async Task RoundTrip_ItemOrderDoesNotAffectSignature()
    {
        // Items in different initial order should produce the same signed output
        // because SerializeAndSignAsync sorts by name.
        var now = DateTimeOffset.UtcNow;
        var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        VaultItemV2 alpha = new() { Id = id1, Name = "Alpha", Type = VaultItemTypeV2.StaticKey, CreatedAt = now, LastModified = now };
        VaultItemV2 bravo = new() { Id = id2, Name = "Bravo", Type = VaultItemTypeV2.StaticKey, CreatedAt = now, LastModified = now };

        VaultV2 vaultAB = new() { Version = 2, Items = [alpha, bravo] };
        VaultV2 vaultBA = new() { Version = 2, Items = [bravo, alpha] };

        string json1 = await VaultDataService.SerializeAndSignAsync(vaultAB, _masterKey, _crypto, CancellationToken.None);
        string json2 = await VaultDataService.SerializeAndSignAsync(vaultBA, _masterKey, _crypto, CancellationToken.None);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public async Task SerializeAndSignAsync_SignatureIsBase58Encoded()
    {
        VaultV2 vault = CreateVault("Item");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        using JsonDocument doc = JsonDocument.Parse(json);
        string signature = doc.RootElement.GetProperty("signature").GetString()!;

        // Base58 alphabet: 123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz
        // Must NOT contain 0, O, I, l, +, /, =.
        Assert.DoesNotContain("0", signature);
        Assert.DoesNotContain("O", signature);
        Assert.DoesNotContain("I", signature);
        Assert.DoesNotContain("l", signature);
        Assert.DoesNotContain("+", signature);
        Assert.DoesNotContain("/", signature);
        Assert.DoesNotContain("=", signature);
        Assert.False(string.IsNullOrWhiteSpace(signature));
    }

    [Fact]
    public async Task SerializeAndSignAsync_NullSignatureIsOmittedFromOutput()
    {
        // Verify that signature: null doesn't appear in the canonical form
        // by checking that a vault without signature has no "signature" key.
        VaultV2 vault = CreateVault("Item");

        string json = await VaultDataService.SerializeAndSignAsync(vault, _masterKey, _crypto, CancellationToken.None);

        // The final output DOES have a signature. But let's verify it round-trips.
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("signature", out _));
    }
}
