using ItchyPassword.Core.Services;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="ErrorLogService"/> entry management and event notifications.
/// </summary>
public sealed class ErrorLogServiceTests
{
    // ── Initial state ──────────────────────────────────────────────────

    [Fact]
    public void InitialState_HasNoEntries()
    {
        var service = new ErrorLogService();

        Assert.Equal(0, service.Count);
        Assert.Empty(service.Entries);
    }

    // ── Log entries ────────────────────────────────────────────────────

    [Fact]
    public void Log_AddsEntry()
    {
        var service = new ErrorLogService();

        service.Log("Test error");

        Assert.Equal(1, service.Count);
        Assert.Equal("Test error", service.Entries[0].Message);
    }

    [Fact]
    public void Log_WithSource_RecordsSource()
    {
        var service = new ErrorLogService();

        service.Log("Error message", source: "TestComponent");

        Assert.Equal("TestComponent", service.Entries[0].Source);
    }

    [Fact]
    public void Log_WithException_RecordsDetails()
    {
        var service = new ErrorLogService();
        var exception = new InvalidOperationException("Something broke");

        service.Log("Error", exception: exception);

        Assert.NotNull(service.Entries[0].Details);
        Assert.Contains("Something broke", service.Entries[0].Details);
    }

    [Fact]
    public void Log_MostRecentFirst()
    {
        var service = new ErrorLogService();

        service.Log("First");
        service.Log("Second");
        service.Log("Third");

        Assert.Equal("Third", service.Entries[0].Message);
        Assert.Equal("Second", service.Entries[1].Message);
        Assert.Equal("First", service.Entries[2].Message);
    }

    [Fact]
    public void Log_SetsTimestamp()
    {
        var service = new ErrorLogService();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        service.Log("Timed error");

        DateTimeOffset after = DateTimeOffset.UtcNow;
        Assert.InRange(service.Entries[0].Timestamp, before, after);
    }

    // ── Max entries cap ────────────────────────────────────────────────

    [Fact]
    public void Log_ExceedingMaxEntries_RemovesOldest()
    {
        var service = new ErrorLogService();

        for (int i = 0; i < ErrorLogService.MaxEntries + 10; i++)
        {
            service.Log($"Error {i}");
        }

        Assert.Equal(ErrorLogService.MaxEntries, service.Count);
        // Most recent should be the last logged.
        Assert.Equal($"Error {ErrorLogService.MaxEntries + 9}", service.Entries[0].Message);
    }

    // ── Clear ──────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var service = new ErrorLogService();
        service.Log("Error 1");
        service.Log("Error 2");

        service.Clear();

        Assert.Equal(0, service.Count);
        Assert.Empty(service.Entries);
    }

    // ── Event notifications ────────────────────────────────────────────

    [Fact]
    public void Log_RaisesOnErrorLoggedEvent()
    {
        var service = new ErrorLogService();
        int eventCount = 0;
        service.OnErrorLogged += () => eventCount++;

        service.Log("Error");

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Clear_RaisesOnErrorLoggedEvent()
    {
        var service = new ErrorLogService();
        service.Log("Error");

        int eventCount = 0;
        service.OnErrorLogged += () => eventCount++;

        service.Clear();

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void MultipleSubscribers_AllNotified()
    {
        var service = new ErrorLogService();
        int count1 = 0;
        int count2 = 0;
        service.OnErrorLogged += () => count1++;
        service.OnErrorLogged += () => count2++;

        service.Log("Error");

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
    }
}
