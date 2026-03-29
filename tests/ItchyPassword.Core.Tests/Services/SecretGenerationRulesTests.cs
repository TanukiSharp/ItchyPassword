using ItchyPassword.Core.Services;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="SecretGenerationRules"/> validation logic.
/// </summary>
public sealed class SecretGenerationRulesTests
{
    // ── Valid rules ─────────────────────────────────────────────────────

    [Fact]
    public void IsValid_DefaultRules_ReturnsTrue()
    {
        var rules = new SecretGenerationRules();
        Assert.True(rules.IsValid(out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_MinimumEqualsTotal_ReturnsTrue()
    {
        var rules = new SecretGenerationRules
        {
            TotalLength = 12,
            MinLowercase = 3,
            MinUppercase = 3,
            MinDigits = 3,
            MinSymbols = 3,
        };
        Assert.True(rules.IsValid(out _));
    }

    // ── Invalid rules ──────────────────────────────────────────────────

    [Fact]
    public void IsValid_ZeroTotalLength_ReturnsFalse()
    {
        var rules = new SecretGenerationRules { TotalLength = 0 };
        Assert.False(rules.IsValid(out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValid_NegativeTotalLength_ReturnsFalse()
    {
        var rules = new SecretGenerationRules { TotalLength = -1 };
        Assert.False(rules.IsValid(out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValid_NegativeMinimum_ReturnsFalse()
    {
        var rules = new SecretGenerationRules { MinLowercase = -1 };
        Assert.False(rules.IsValid(out _));
    }

    [Fact]
    public void IsValid_SumExceedsTotal_ReturnsFalse()
    {
        var rules = new SecretGenerationRules
        {
            TotalLength = 4,
            MinLowercase = 3,
            MinUppercase = 3,
            MinDigits = 3,
            MinSymbols = 3,
        };
        Assert.False(rules.IsValid(out string? error));
        Assert.Contains("exceeds", error);
    }

    [Fact]
    public void IsValid_SymbolsRequiredButEmptyAlphabet_ReturnsFalse()
    {
        var rules = new SecretGenerationRules
        {
            MinSymbols = 1,
            SymbolAlphabet = string.Empty,
        };
        Assert.False(rules.IsValid(out _));
    }

    [Fact]
    public void IsValid_AllMinimumsZeroButTotalAboveZero_ReturnsFalse()
    {
        var rules = new SecretGenerationRules
        {
            TotalLength = 10,
            MinLowercase = 0,
            MinUppercase = 0,
            MinDigits = 0,
            MinSymbols = 0,
        };
        Assert.False(rules.IsValid(out string? error));
        Assert.Contains("at least one", error, StringComparison.OrdinalIgnoreCase);
    }

    // ── MinimumRequired calculation ────────────────────────────────────

    [Fact]
    public void MinimumRequired_SumsAllMinimums()
    {
        var rules = new SecretGenerationRules
        {
            MinLowercase = 2,
            MinUppercase = 3,
            MinDigits = 4,
            MinSymbols = 5,
        };
        Assert.Equal(14, rules.MinimumRequired);
    }

    [Fact]
    public void MinimumRequired_DefaultsTo12()
    {
        var rules = new SecretGenerationRules();
        Assert.Equal(12, rules.MinimumRequired);
    }

    // ── Selective character classes ─────────────────────────────────────

    [Fact]
    public void IsValid_OnlyLowercase_ReturnsTrue()
    {
        var rules = new SecretGenerationRules
        {
            TotalLength = 20,
            MinLowercase = 5,
            MinUppercase = 0,
            MinDigits = 0,
            MinSymbols = 0,
        };
        Assert.True(rules.IsValid(out _));
    }

    [Fact]
    public void IsValid_ExactFit_AllFourClasses_ReturnsTrue()
    {
        var rules = new SecretGenerationRules
        {
            TotalLength = 4,
            MinLowercase = 1,
            MinUppercase = 1,
            MinDigits = 1,
            MinSymbols = 1,
        };
        Assert.True(rules.IsValid(out _));
    }
}
