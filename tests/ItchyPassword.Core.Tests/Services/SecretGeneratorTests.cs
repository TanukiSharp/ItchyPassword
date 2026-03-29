using ItchyPassword.Core.Services;
using ItchyPassword.Core.Tests.Crypto;
using Microsoft.Playwright;

namespace ItchyPassword.Core.Tests.Services;

public class SecretGeneratorTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IPage _page = null!;
    private PlaywrightCryptoService _crypto = null!;

    public SecretGeneratorTests(PlaywrightFixture fixture)
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

    [Fact]
    public async Task GenerateAsync_DefaultRules_ReturnsNonEmptyString()
    {
        var pool = new RandomBytePool(_crypto);
        var rules = new SecretGenerationRules();

        string result = await SecretGenerator.GenerateAsync(rules, pool, CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(result), $"Result should not be empty. Length={result.Length}");
        Assert.Equal(rules.TotalLength, result.Length);
    }

    [Fact]
    public async Task GenerateAsync_DefaultRules_ContainsAllCharClasses()
    {
        var pool = new RandomBytePool(_crypto);
        var rules = new SecretGenerationRules();

        string result = await SecretGenerator.GenerateAsync(rules, pool, CancellationToken.None);

        Assert.Contains(result, c => char.IsLower(c));
        Assert.Contains(result, c => char.IsUpper(c));
        Assert.Contains(result, c => char.IsDigit(c));
        Assert.Contains(result, c => rules.SymbolAlphabet.Contains(c));
    }

    [Fact]
    public async Task GenerateAsync_SmallLength_Works()
    {
        var pool = new RandomBytePool(_crypto);
        var rules = new SecretGenerationRules
        {
            TotalLength = 12,
            MinLowercase = 3,
            MinUppercase = 3,
            MinDigits = 3,
            MinSymbols = 3
        };

        string result = await SecretGenerator.GenerateAsync(rules, pool, CancellationToken.None);

        Assert.Equal(12, result.Length);

        // Check each character individually for null.
        for (int i = 0; i < result.Length; i++)
        {
            Assert.True(result[i] != '\0', $"Char at index {i} is null (0x0000). Full ints: [{string.Join(",", result.Select(c => (int)c))}]");
        }
    }

    [Fact]
    public async Task GenerateAsync_DefaultLength_NoNullChars()
    {
        var pool = new RandomBytePool(_crypto);
        var rules = new SecretGenerationRules();

        for (int run = 0; run < 50; run++)
        {
            string result = await SecretGenerator.GenerateAsync(rules, pool, CancellationToken.None);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.True(result[i] != '\0',
                    $"Run {run}: Char at index {i} is null. Full ints: [{string.Join(",", result.Select(c => (int)c))}]");
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_RepeatedCalls_AlwaysReturnNonEmpty()
    {
        var pool = new RandomBytePool(_crypto);
        var rules = new SecretGenerationRules();

        for (int i = 0; i < 20; i++)
        {
            string result = await SecretGenerator.GenerateAsync(rules, pool, CancellationToken.None);
            Assert.False(string.IsNullOrEmpty(result), $"Iteration {i}: result was empty");
            Assert.Equal(rules.TotalLength, result.Length);
        }
    }
}
