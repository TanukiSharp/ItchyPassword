using ItchyPassword.Core.Services;
using System.Collections.Frozen;

namespace ItchyPassword.Core.Tests.Services;

public sealed class AlphabetValidatorTests
{
    // ───── FindDuplicates ─────

    [Fact]
    public void FindDuplicates_NoDuplicates_ReturnsEmpty()
    {
        FrozenDictionary<char, int> result = AlphabetValidator.FindDuplicates("abcdef");
        Assert.Empty(result);
    }

    [Fact]
    public void FindDuplicates_WithDuplicates_ReturnsCorrectCounts()
    {
        FrozenDictionary<char, int> result = AlphabetValidator.FindDuplicates("aabbc");

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result['a']);
        Assert.Equal(2, result['b']);
    }

    [Fact]
    public void FindDuplicates_MultipleSameChar_ReturnsCount()
    {
        FrozenDictionary<char, int> result = AlphabetValidator.FindDuplicates("aaaa");

        Assert.Single(result);
        Assert.Equal(4, result['a']);
    }

    [Fact]
    public void FindDuplicates_EmptyString_ReturnsEmpty()
    {
        FrozenDictionary<char, int> result = AlphabetValidator.FindDuplicates("");
        Assert.Empty(result);
    }

    [Fact]
    public void FindDuplicates_Null_ReturnsEmpty()
    {
        FrozenDictionary<char, int> result = AlphabetValidator.FindDuplicates(null!);
        Assert.Empty(result);
    }

    // ───── Deduplicate ─────

    [Fact]
    public void Deduplicate_RemovesDuplicatesKeepsOrder()
    {
        string result = AlphabetValidator.Deduplicate("abcabc");
        Assert.Equal("abc", result);
    }

    [Fact]
    public void Deduplicate_NoDuplicates_ReturnsSame()
    {
        string result = AlphabetValidator.Deduplicate("abcdef");
        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void Deduplicate_Empty_ReturnsEmpty()
    {
        string result = AlphabetValidator.Deduplicate("");
        Assert.Equal("", result);
    }

    [Fact]
    public void Deduplicate_PreservesFirstOccurrence()
    {
        string result = AlphabetValidator.Deduplicate("baba");
        Assert.Equal("ba", result);
    }

    // ───── Real-world alphabet ─────

    [Fact]
    public void DefaultAlphabet_HasNoDuplicates()
    {
        // The default alphabet from StaticKeyDataConstants should have no duplicates.
        string defaultAlphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        Assert.Empty(AlphabetValidator.FindDuplicates(defaultAlphabet));
    }
}
