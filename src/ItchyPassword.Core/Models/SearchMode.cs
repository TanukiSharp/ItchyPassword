namespace ItchyPassword.Core.Models;

/// <summary>
/// Defines how the search query is matched against text.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Substring match (case-insensitive, diacritics-insensitive).
    /// </summary>
    Contains = 0,

    /// <summary>
    /// Greedy fuzzy match: characters of the query appear in order within the text,
    /// grouping longest consecutive runs for highlighting (case-insensitive, diacritics-insensitive).
    /// </summary>
    Fuzzy = 1,

    /// <summary>
    /// Full string equality (case-insensitive, diacritics-insensitive).
    /// </summary>
    Exact = 2,
}
