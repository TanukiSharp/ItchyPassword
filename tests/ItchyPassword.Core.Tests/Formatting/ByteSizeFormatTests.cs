using ItchyPassword.Core.Formatting;

namespace ItchyPassword.Core.Tests.Formatting;

/// <summary>
/// Tests for <see cref="ByteSizeFormat.ToHumanReadable"/>.
/// </summary>
public sealed class ByteSizeFormatTests
{
    // ── Bytes ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1, "1 bytes")]
    [InlineData(512, "512 bytes")]
    [InlineData(1023, "1023 bytes")]
    public void ToHumanReadable_Bytes(int count, string expected)
    {
        Assert.Equal(expected, ByteSizeFormat.ToHumanReadable(count));
    }

    // ── Kilobytes ──────────────────────────────────────────────────────

    [Fact]
    public void ToHumanReadable_ExactlyOneKB()
    {
        Assert.Equal("1.00 KB", ByteSizeFormat.ToHumanReadable(1024));
    }

    [Fact]
    public void ToHumanReadable_FractionalKB()
    {
        Assert.Equal("1.50 KB", ByteSizeFormat.ToHumanReadable(1536));
    }

    // ── Megabytes ──────────────────────────────────────────────────────

    [Fact]
    public void ToHumanReadable_ExactlyOneMB()
    {
        Assert.Equal("1.00 MB", ByteSizeFormat.ToHumanReadable(1024 * 1024));
    }

    [Fact]
    public void ToHumanReadable_FractionalMB()
    {
        int bytes = (int)(5.5 * 1024 * 1024);
        Assert.Equal("5.50 MB", ByteSizeFormat.ToHumanReadable(bytes));
    }

    // ── Gigabytes ──────────────────────────────────────────────────────

    [Fact]
    public void ToHumanReadable_ExactlyOneGB()
    {
        Assert.Equal("1.00 GB", ByteSizeFormat.ToHumanReadable(1024 * 1024 * 1024));
    }

    // ── Boundary values ────────────────────────────────────────────────

    [Fact]
    public void ToHumanReadable_JustBelowKB()
    {
        Assert.Equal("1023 bytes", ByteSizeFormat.ToHumanReadable(1023));
    }

    [Fact]
    public void ToHumanReadable_JustBelowMB()
    {
        string result = ByteSizeFormat.ToHumanReadable(1024 * 1024 - 1);
        Assert.EndsWith("KB", result);
    }

    // ── Correct unit selection ──────────────────────────────────────────

    [Fact]
    public void ToHumanReadable_ByteRange_DoesNotShowKB()
    {
        string result = ByteSizeFormat.ToHumanReadable(500);
        Assert.EndsWith("bytes", result);
    }

    [Fact]
    public void ToHumanReadable_KBRange_DoesNotShowMB()
    {
        string result = ByteSizeFormat.ToHumanReadable(50_000);
        Assert.EndsWith("KB", result);
    }

    [Fact]
    public void ToHumanReadable_MBRange_DoesNotShowGB()
    {
        string result = ByteSizeFormat.ToHumanReadable(50_000_000);
        Assert.EndsWith("MB", result);
    }
}
