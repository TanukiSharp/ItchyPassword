namespace ItchyPassword.Client.Components;

/// <summary>
/// Describes a single action button in a <see cref="ConfirmationModal"/>.
/// </summary>
/// <param name="Label">Text displayed on the button.</param>
/// <param name="CssClass">CSS class(es) applied to the button (e.g. "button primary", "button danger").</param>
/// <param name="OnClick">Async callback invoked when the button is clicked.</param>
public record ModalAction(string Label, string CssClass, Func<Task> OnClick);
