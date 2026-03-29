using ItchyPassword.Core.Encoding;
using ItchyPassword.Core.Services;

namespace ItchyPassword.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="TotpGenerator"/> using RFC 6238 test vectors (SHA-1).
/// </summary>
public sealed class TotpGeneratorTests
{
    // The RFC 6238 test secret for HMAC-SHA1 is ASCII "12345678901234567890" (20 bytes).
    private static readonly byte[] Rfc6238SecretSha1 = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── RFC 6238 Appendix B test vectors (SHA-1) ───────────────────────

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1111111111, "050471")]
    [InlineData(1234567890, "005924")]
    [InlineData(2000000000, "279037")]
    public void GenerateCode_RFC6238TestVectors_SHA1(long unixSeconds, string expectedCode)
    {
        DateTime timestamp = UnixEpoch.AddSeconds(unixSeconds);

        string code = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp);

        Assert.Equal(expectedCode, code);
    }

    // ── Digit count behavior ───────────────────────────────────────────

    [Fact]
    public void GenerateCode_8Digits_ProducesExpectedLength()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(59);

        string code = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp, 8);

        Assert.Equal(8, code.Length);
        // 8-digit code for time=59 with SHA-1: 94287082
        Assert.Equal("94287082", code);
    }

    [Fact]
    public void GenerateCode_1Digit_ProducesExpectedLength()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(59);

        string code = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp, 1);

        Assert.Single(code);
    }

    [Fact]
    public void GenerateCode_CodeIsPaddedWithLeadingZeros()
    {
        // The code for time=1234567890 is "005924" - starts with zeros.
        DateTime timestamp = UnixEpoch.AddSeconds(1234567890);
        string code = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp);
        Assert.Equal(6, code.Length);
        Assert.StartsWith("00", code);
    }

    // ── Remaining seconds ──────────────────────────────────────────────

    [Fact]
    public void GetRemainingSeconds_AtEpoch_Returns30()
    {
        int remaining = TotpGenerator.GetRemainingSeconds(UnixEpoch);
        Assert.Equal(30, remaining);
    }

    [Fact]
    public void GetRemainingSeconds_At15Seconds_Returns15()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(15);
        int remaining = TotpGenerator.GetRemainingSeconds(timestamp);
        Assert.Equal(15, remaining);
    }

    [Fact]
    public void GetRemainingSeconds_At29Seconds_Returns1()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(29);
        int remaining = TotpGenerator.GetRemainingSeconds(timestamp);
        Assert.Equal(1, remaining);
    }

    [Fact]
    public void GetRemainingSeconds_At30Seconds_Returns30()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(30);
        int remaining = TotpGenerator.GetRemainingSeconds(timestamp);
        Assert.Equal(30, remaining);
    }

    [Fact]
    public void GetRemainingSeconds_AlwaysBetween1And30()
    {
        // Check multiple timestamps.
        for (int i = 0; i < 60; i++)
        {
            DateTime timestamp = UnixEpoch.AddSeconds(i);
            int remaining = TotpGenerator.GetRemainingSeconds(timestamp);
            Assert.InRange(remaining, 1, 30);
        }
    }

    // ── Input validation ───────────────────────────────────────────────

    [Fact]
    public void GenerateCode_EmptySecret_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TotpGenerator.GenerateCode([], DateTime.UtcNow));
    }

    [Fact]
    public void GenerateCode_NullSecret_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TotpGenerator.GenerateCode(null!, DateTime.UtcNow));
    }

    [Fact]
    public void GenerateCode_DigitCountTooLow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TotpGenerator.GenerateCode(Rfc6238SecretSha1, DateTime.UtcNow, 0));
    }

    [Fact]
    public void GenerateCode_DigitCountTooHigh_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TotpGenerator.GenerateCode(Rfc6238SecretSha1, DateTime.UtcNow, 9));
    }

    // ── Determinism ────────────────────────────────────────────────────

    [Fact]
    public void GenerateCode_SameInputs_ProduceSameOutput()
    {
        DateTime timestamp = UnixEpoch.AddSeconds(1234567890);

        string code1 = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp);
        string code2 = TotpGenerator.GenerateCode(Rfc6238SecretSha1, timestamp);

        Assert.Equal(code1, code2);
    }

    [Fact]
    public void GenerateCode_DifferentTimeSteps_ProduceDifferentCodes()
    {
        // Time=59 (step 1) and Time=90 (step 3) should produce different codes.
        string code1 = TotpGenerator.GenerateCode(Rfc6238SecretSha1, UnixEpoch.AddSeconds(59));
        string code2 = TotpGenerator.GenerateCode(Rfc6238SecretSha1, UnixEpoch.AddSeconds(90));

        Assert.NotEqual(code1, code2);
    }

    // ── TOTP with real-world style Base32 secret ───────────────────────

    [Fact]
    public void GenerateCode_Base32DecodedSecret_MatchesRFC()
    {
        // The RFC test secret "12345678901234567890" base32-encoded is "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ".
        byte[] secretFromBase32 = Base32.Decode("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");
        DateTime timestamp = UnixEpoch.AddSeconds(59);

        string code = TotpGenerator.GenerateCode(secretFromBase32, timestamp);

        Assert.Equal("287082", code);
    }
}
