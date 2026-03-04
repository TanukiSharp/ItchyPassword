namespace ItchyPassword.Core.Models;

/// <summary>
/// Represents a metadata entry that matched the search query.
/// </summary>
public readonly record struct MetadataMatch(
    string Key,
    string Value,
    SearchResult KeyResult,
    SearchResult ValueResult
);
