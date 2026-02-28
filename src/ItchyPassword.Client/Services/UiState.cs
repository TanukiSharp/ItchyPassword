namespace ItchyPassword.Client.Services;

/// <summary>
/// Holds cross-page UI state that must survive Blazor navigation but is not
/// persisted to storage. Keep this class lean — add fields only for transient
/// presentation concerns that do not belong in domain services.
/// </summary>
public class UiState
{
    /// <summary>
    /// Gets or sets the search query used to filter vault items.
    /// Preserved across page navigations so the user does not lose their filter.
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;
}
