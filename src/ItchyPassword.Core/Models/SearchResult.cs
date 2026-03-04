using System.Collections.Immutable;

namespace ItchyPassword.Core.Models;

/// <summary>
/// Represents a contiguous range of matched characters within the searched text.
/// </summary>
/// <param name="Position">Zero-based start index in the original text.</param>
/// <param name="Length">Number of consecutive matched characters.</param>
public readonly record struct MatchRange(int Position, int Length);

/// <summary>
/// The result of a search operation against a single string.
/// </summary>
/// <param name="IsMatch">Whether the search query matched the text.</param>
/// <param name="Ranges">Highlight ranges within the original text. Empty when <see cref="IsMatch"/> is <c>false</c>.</param>
public sealed record SearchResult(bool IsMatch, IReadOnlyList<MatchRange> Ranges)
{
    /// <summary>
    /// A singleton representing no match.
    /// </summary>
    public static readonly SearchResult NoMatch = new(false, []);
}
