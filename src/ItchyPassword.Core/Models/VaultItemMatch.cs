namespace ItchyPassword.Core.Models;

/// <summary>
/// Represents a vault item with its search match results.
/// </summary>
public readonly record struct VaultItemMatch(
    VaultItemV2 Item,
    SearchResult NameResult,
    List<MetadataMatch> MetadataMatches
)
{
    /// <summary>
    /// Creates a match with no search results (used when there is no active query).
    /// </summary>
    public VaultItemMatch(VaultItemV2 item)
        : this(item, SearchResult.NoMatch, [])
    {
    }
}
