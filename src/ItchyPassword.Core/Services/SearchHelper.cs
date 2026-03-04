using ItchyPassword.Core.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Provides search matching with three modes: Contains, Fuzzy, and Exact.
/// All comparisons are case-insensitive and diacritics-insensitive using <see cref="CompareInfo"/>.
/// </summary>
public static class SearchHelper
{
    private static readonly CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Matches <paramref name="query"/> against <paramref name="text"/> using the specified <paramref name="mode"/>.
    /// </summary>
    public static SearchResult Match(string text, string query, SearchMode mode)
    {
        if (string.IsNullOrEmpty(query))
        {
            return SearchResult.NoMatch;
        }

        if (string.IsNullOrEmpty(text))
        {
            return SearchResult.NoMatch;
        }

        return mode switch
        {
            SearchMode.Contains => MatchContains(text, query),
            SearchMode.Fuzzy => MatchFuzzy(text, query),
            SearchMode.Exact => MatchExact(text, query),
            _ => SearchResult.NoMatch,
        };
    }

    private static SearchResult MatchContains(string text, string query)
    {
        int index = compare.IndexOf(text.AsSpan(), query.AsSpan(), options, out int matchLength);

        if (index < 0)
        {
            return SearchResult.NoMatch;
        }

        return new SearchResult(true, [new MatchRange(index, matchLength)]);
    }

    private static SearchResult MatchExact(string text, string query)
    {
        if (compare.Compare(text, query, options) != 0)
        {
            return SearchResult.NoMatch;
        }

        return new SearchResult(true, [new MatchRange(0, text.Length)]);
    }

    /// <summary>
    /// Greedy fuzzy match: tries the longest prefix of the query that appears as a substring,
    /// records the match range, then recurses on the remainder.
    /// </summary>
    private static SearchResult MatchFuzzy(string text, string query)
    {
        List<MatchRange> ranges = [];

        if (FuzzyMatchRecursive(text.AsSpan(), query.AsSpan(), 0, ranges))
        {
            return new SearchResult(true, new ReadOnlyCollection<MatchRange>(ranges));
        }

        return SearchResult.NoMatch;
    }

    /// <summary>
    /// Recursively finds the longest prefix of <paramref name="remainingQuery"/> that matches
    /// in <paramref name="text"/> starting from <paramref name="searchStartIndex"/>,
    /// then recurses with the unmatched remainder.
    /// </summary>
    private static bool FuzzyMatchRecursive(ReadOnlySpan<char> text, ReadOnlySpan<char> remainingQuery, int searchStartIndex, List<MatchRange> ranges)
    {
        if (remainingQuery.Length == 0)
        {
            return true;
        }

        if (searchStartIndex >= text.Length)
        {
            return false;
        }

        ReadOnlySpan<char> searchSlice = text[searchStartIndex..];

        // Try longest prefix first (greedy), then shorten.
        for (int prefixLen = remainingQuery.Length; prefixLen >= 1; prefixLen--)
        {
            ReadOnlySpan<char> prefix = remainingQuery[..prefixLen];
            int foundIndex = compare.IndexOf(searchSlice, prefix, options, out int matchLength);

            if (foundIndex < 0)
            {
                continue;
            }

            int absoluteIndex = searchStartIndex + foundIndex;
            ranges.Add(new MatchRange(absoluteIndex, matchLength));

            // Recurse with the rest of the query, searching after the current match.
            int nextSearchStart = absoluteIndex + matchLength;
            ReadOnlySpan<char> nextQuery = remainingQuery[prefixLen..];

            return FuzzyMatchRecursive(text, nextQuery, nextSearchStart, ranges);
        }

        return false;
    }
}
