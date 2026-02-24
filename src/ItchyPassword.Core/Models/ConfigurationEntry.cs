namespace ItchyPassword.Core.Models;

/// <summary>
/// Describes the visual and behavioral kind of a configuration entry.
/// </summary>
public enum ConfigurationEntryKind
{
    /// <summary>
    /// A plain text input field.
    /// </summary>
    Text,

    /// <summary>
    /// A masked input field for secrets (tokens, passwords).
    /// </summary>
    Secret,

    /// <summary>
    /// A dropdown/select field with predefined options.
    /// </summary>
    Dropdown,

    /// <summary>
    /// A read-only display field (informational, not editable by the user).
    /// </summary>
    ReadOnly,
}

/// <summary>
/// Represents a single option in a <see cref="ConfigurationEntryKind.Dropdown"/> entry.
/// </summary>
/// <param name="Value">The internal value stored when this option is selected.</param>
/// <param name="Label">The user-visible display label for this option.</param>
public record DropdownOption(string Value, string Label);

/// <summary>
/// Describes a single configuration field for a vault connector.
/// Provides metadata for the settings UI (label, description, kind, visibility)
/// while also tracking the current value and its localStorage key for persistence.
/// </summary>
public class ConfigurationEntry
{
    /// <summary>
    /// Gets the unique key that identifies this entry within a connector's configuration list.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the user-visible label displayed next to the input control.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets an optional description or help text displayed below the input control.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the visual and behavioral kind of this entry (text, secret, dropdown, etc.).
    /// </summary>
    public ConfigurationEntryKind Kind { get; init; } = ConfigurationEntryKind.Text;

    /// <summary>
    /// Gets or sets the current in-memory value of this entry.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets the default value applied when no stored value exists.
    /// </summary>
    public string DefaultValue { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional placeholder text shown when the input is empty.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// Gets the list of selectable options when <see cref="Kind"/> is <see cref="ConfigurationEntryKind.Dropdown"/>.
    /// </summary>
    public IReadOnlyList<DropdownOption> Options { get; init; } = [];

    /// <summary>
    /// Gets the key of another <see cref="ConfigurationEntry"/> that controls this entry's visibility.
    /// When set, this entry is only visible when the referenced entry's value equals <see cref="VisibleWhenValue"/>.
    /// </summary>
    public string? VisibleWhenKey { get; init; }

    /// <summary>
    /// Gets the value that <see cref="VisibleWhenKey"/>'s entry must have for this entry to be visible.
    /// </summary>
    public string? VisibleWhenValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry must have a non-empty value for the connector
    /// to be considered configured.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets the localStorage key used to persist this entry's value.
    /// When <c>null</c>, the entry is transient and not persisted.
    /// </summary>
    public string? StorageKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry's stored value should be encrypted with the master key.
    /// Only meaningful when <see cref="StorageKey"/> is not <c>null</c>.
    /// </summary>
    public bool IsEncrypted { get; init; }
}
