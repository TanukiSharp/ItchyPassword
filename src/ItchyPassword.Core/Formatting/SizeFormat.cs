namespace ItchyPassword.Core.Formatting;

public static class ByteSizeFormat
{
    public const int KB = 1024;
    public const int MB = 1024 * KB;
    public const int GB = 1024 * MB;

    /// <summary>
    /// Formats a byte count as a human-readable size string (bytes, KB, MB, GB).
    /// </summary>
    public static string ToHumanReadable(int count)
    {
        return count switch
        {
            >= GB => $"{count / (double)GB:F2} GB",
            >= MB => $"{count / (double)MB:F2} MB",
            >= KB => $"{count / (double)KB:F1} KB",
            _ => $"{count} bytes",
        };
    }
}
