using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;
using System.Text.Json.Nodes;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="VaultMigrationService"/> structural migration detection and conversion.
/// </summary>
public sealed class VaultMigrationServiceTests
{
    // ── IsLegacyVault detection ────────────────────────────────────────

    [Fact]
    public void IsLegacyVault_V1Format_ReturnsTrue()
    {
        // V1 vaults are plain key-value objects without "version" and "items".
        string v1Json = """
        {
            "GitHub": {
                "password": {
                    "public": "github.com/user",
                    "version": 1,
                    "length": 64,
                    "alphabet": "abc",
                    "datetime": "2023-01-01"
                }
            }
        }
        """;

        Assert.True(VaultMigrationService.IsLegacyVault(v1Json));
    }

    [Fact]
    public void IsLegacyVault_V2Format_ReturnsFalse()
    {
        string v2Json = """{"version": 2, "items": []}""";

        Assert.False(VaultMigrationService.IsLegacyVault(v2Json));
    }

    [Fact]
    public void IsLegacyVault_InvalidJson_ReturnsFalse()
    {
        Assert.False(VaultMigrationService.IsLegacyVault("not json at all"));
    }

    [Fact]
    public void IsLegacyVault_ArrayJson_ReturnsFalse()
    {
        Assert.False(VaultMigrationService.IsLegacyVault("[1, 2, 3]"));
    }

    [Fact]
    public void IsLegacyVault_EmptyObject_ReturnsTrue()
    {
        // An empty object without "version"/"items" is treated as legacy.
        Assert.True(VaultMigrationService.IsLegacyVault("{}"));
    }

    [Fact]
    public void IsLegacyVault_ObjectWithVersionOnly_ReturnsTrue()
    {
        // Has "version" but no "items" - treated as legacy.
        Assert.True(VaultMigrationService.IsLegacyVault("""{"version": 2}"""));
    }

    [Fact]
    public void IsLegacyVault_ObjectWithItemsOnly_ReturnsTrue()
    {
        // Has "items" but no "version" - treated as legacy.
        Assert.True(VaultMigrationService.IsLegacyVault("""{"items": []}"""));
    }
}
