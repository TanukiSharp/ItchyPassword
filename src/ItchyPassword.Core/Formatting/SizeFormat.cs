using System.Globalization;

namespace ItchyPassword.Core.Formatting;

public static class ByteSizeFormat
{
    public const int KB = 1024;
    public const int MB = 1024 * KB;
    public const int GB = 1024 * MB;

    /// <summary>
    /// Formats a byte count as a human-readable size string (bytes, KB, MB, GB).
    /// Always uses '.' as the decimal separator regardless of locale.
    /// </summary>
    public static string ToHumanReadable(int count)
    {
        return count switch
        {
            >= GB => Format(count, GB, "GB"),
            >= MB => Format(count, MB, "MB"),
            >= KB => Format(count, KB, "KB"),
            _ => $"{count} bytes",
        };
    }

    private static string Format(int value, int unitValue, string unitText)
    {
        return string.Format(CultureInfo.InvariantCulture, $"{{0:F2}} {unitText}", value / (double)unitValue);
    }
}
