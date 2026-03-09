namespace ItchyPassword.Client.Components;

/// <summary>
/// Represents a single option in a <see cref="RichSelect{TValue}"/> dropdown.
/// </summary>
/// <typeparam name="TValue">The type of the option's value.</typeparam>
/// <param name="Value">The underlying value for this option.</param>
/// <param name="Label">The primary display text.</param>
/// <param name="Hint">An optional secondary description displayed below the label.</param>
/// <param name="Disabled">Whether this option is grayed out and non-selectable.</param>
public record RichSelectOption<TValue>(TValue Value, string Label, string Hint = "", bool Disabled = false);
