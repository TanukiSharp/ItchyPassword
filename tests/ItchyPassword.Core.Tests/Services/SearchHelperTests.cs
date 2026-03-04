using ItchyPassword.Core.Models;
using ItchyPassword.Core.Services;

namespace ItchyPassword.Core.Tests.Services;

public sealed class SearchHelperTests
{
    // ───── Contains mode ─────

    [Fact]
    public void Contains_FindsSubstring()
    {
        SearchResult result = SearchHelper.Match("Hello World", "world", SearchMode.Contains);

        Assert.True(result.IsMatch);
        Assert.Single(result.Ranges);
        Assert.Equal(6, result.Ranges[0].Position);
        Assert.Equal(5, result.Ranges[0].Length);
    }

    [Fact]
    public void Contains_IgnoresDiacritics()
    {
        SearchResult result = SearchHelper.Match("café latte", "cafe", SearchMode.Contains);

        Assert.True(result.IsMatch);
        Assert.Single(result.Ranges);
        Assert.Equal(0, result.Ranges[0].Position);
        Assert.Equal(4, result.Ranges[0].Length);
    }

    [Fact]
    public void Contains_ReturnsNoMatch_WhenNotFound()
    {
        SearchResult result = SearchHelper.Match("Hello", "xyz", SearchMode.Contains);

        Assert.False(result.IsMatch);
        Assert.Empty(result.Ranges);
    }

    [Fact]
    public void Contains_EmptyQuery_ReturnsNoMatch()
    {
        SearchResult result = SearchHelper.Match("Hello", "", SearchMode.Contains);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Contains_EmptyText_ReturnsNoMatch()
    {
        SearchResult result = SearchHelper.Match("", "test", SearchMode.Contains);
        Assert.False(result.IsMatch);
    }

    // ───── Exact mode ─────

    [Fact]
    public void Exact_MatchesFullString_CaseInsensitive()
    {
        SearchResult result = SearchHelper.Match("Hello", "hello", SearchMode.Exact);

        Assert.True(result.IsMatch);
        Assert.Single(result.Ranges);
        Assert.Equal(0, result.Ranges[0].Position);
        Assert.Equal(5, result.Ranges[0].Length);
    }

    [Fact]
    public void Exact_MatchesFullString_DiacriticsInsensitive()
    {
        SearchResult result = SearchHelper.Match("café", "cafe", SearchMode.Exact);

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Exact_DoesNotMatchSubstring()
    {
        SearchResult result = SearchHelper.Match("Hello World", "Hello", SearchMode.Exact);
        Assert.False(result.IsMatch);
    }

    // ───── Fuzzy mode ─────

    [Fact]
    public void Fuzzy_MatchesContiguousCharacters()
    {
        SearchResult result = SearchHelper.Match("password", "pass", SearchMode.Fuzzy);

        Assert.True(result.IsMatch);
        Assert.Single(result.Ranges);
        Assert.Equal(0, result.Ranges[0].Position);
        Assert.Equal(4, result.Ranges[0].Length);
    }

    [Fact]
    public void Fuzzy_MatchesScatteredCharacters()
    {
        // "pwd" should match "p" in "password" (pos 0), "w" in "password" (pos 4), "d" in "password" (pos 6)
        SearchResult result = SearchHelper.Match("password", "pwd", SearchMode.Fuzzy);

        Assert.True(result.IsMatch);
        Assert.True(result.Ranges.Count >= 1); // At least one range
    }

    [Fact]
    public void Fuzzy_GreedyLongestPrefixFirst()
    {
        // "ab" in "aXab": greedy tries "ab" first, finds it at index 2 as a single range.
        SearchResult result = SearchHelper.Match("aXab", "ab", SearchMode.Fuzzy);

        Assert.True(result.IsMatch);
        Assert.Single(result.Ranges);
        Assert.Equal(2, result.Ranges[0].Position);
        Assert.Equal(2, result.Ranges[0].Length);
    }

    [Fact]
    public void Fuzzy_IgnoresDiacritics()
    {
        SearchResult result = SearchHelper.Match("résumé", "resume", SearchMode.Fuzzy);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Fuzzy_ReturnsNoMatch_WhenCharsMissing()
    {
        SearchResult result = SearchHelper.Match("abc", "abcz", SearchMode.Fuzzy);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Fuzzy_CaseInsensitive()
    {
        SearchResult result = SearchHelper.Match("MyPassword", "myp", SearchMode.Fuzzy);
        Assert.True(result.IsMatch);
    }

    // ───── Edge cases ─────

    [Fact]
    public void Match_NullQuery_ReturnsNoMatch()
    {
        SearchResult result = SearchHelper.Match("text", null!, SearchMode.Contains);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Match_NullText_ReturnsNoMatch()
    {
        SearchResult result = SearchHelper.Match(null!, "query", SearchMode.Contains);
        Assert.False(result.IsMatch);
    }

    [Theory]
    [InlineData(SearchMode.Contains)]
    [InlineData(SearchMode.Fuzzy)]
    [InlineData(SearchMode.Exact)]
    public void AllModes_SingleCharQuery_Works(SearchMode mode)
    {
        SearchResult result = SearchHelper.Match("Hello", "h", mode);

        if (mode == SearchMode.Exact)
        {
            Assert.False(result.IsMatch); // "h" != "Hello"
        }
        else
        {
            Assert.True(result.IsMatch);
        }
    }
}
