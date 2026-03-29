using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Core.Tests.Encoding;

/// <summary>
/// Tests for <see cref="Base32"/> decoding using RFC 4648 test vectors.
/// </summary>
public sealed class Base32Tests
{
    // ── RFC 4648 test vectors ──────────────────────────────────────────

    [Theory]
    [InlineData("", new byte[0])]
    [InlineData("MY", new byte[] { 0x66 })]                             // "f"
    [InlineData("MZXQ", new byte[] { 0x66, 0x6F })]                     // "fo"
    [InlineData("MZXW6", new byte[] { 0x66, 0x6F, 0x6F })]              // "foo"
    [InlineData("MZXW6YQ", new byte[] { 0x66, 0x6F, 0x6F, 0x62 })]     // "foob"
    [InlineData("MZXW6YTB", new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61 })] // "fooba"
    [InlineData("MZXW6YTBOI", new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 })] // "foobar"
    public void Decode_RFC4648TestVectors(string input, byte[] expected)
    {
        byte[] result = Base32.Decode(input);
        Assert.Equal(expected, result);
    }

    // ── Case insensitivity ─────────────────────────────────────────────

    [Fact]
    public void Decode_LowercaseInput_SameAsUppercase()
    {
        byte[] upper = Base32.Decode("MZXW6YTBOI");
        byte[] lower = Base32.Decode("mzxw6ytboi");
        Assert.Equal(upper, lower);
    }

    [Fact]
    public void Decode_MixedCaseInput_SameAsUppercase()
    {
        byte[] upper = Base32.Decode("MZXW6YTBOI");
        byte[] mixed = Base32.Decode("MzXw6yTbOi");
        Assert.Equal(upper, mixed);
    }

    // ── Padding handling ───────────────────────────────────────────────

    [Fact]
    public void Decode_WithPadding_StrippedCorrectly()
    {
        // "MY======" is the padded form of "MY" which decodes to "f".
        byte[] result = Base32.Decode("MY======");
        Assert.Equal([0x66], result);
    }

    [Fact]
    public void Decode_WithPartialPadding_StrippedCorrectly()
    {
        // MZXW6YQ= is padded form of "foob".
        byte[] result = Base32.Decode("MZXW6YQ=");
        Assert.Equal([0x66, 0x6F, 0x6F, 0x62], result);
    }

    // ── Whitespace handling ────────────────────────────────────────────

    [Fact]
    public void Decode_WithWhitespace_StrippedCorrectly()
    {
        byte[] result = Base32.Decode("MZXW 6YTB OI");
        byte[] expected = System.Text.Encoding.ASCII.GetBytes("foobar");
        Assert.Equal(expected, result);
    }

    // ── Edge cases ─────────────────────────────────────────────────────

    [Fact]
    public void Decode_NullInput_ReturnsEmpty()
    {
        Assert.Empty(Base32.Decode(null!));
    }

    [Fact]
    public void Decode_WhitespaceOnly_ReturnsEmpty()
    {
        Assert.Empty(Base32.Decode("   "));
    }

    [Fact]
    public void Decode_PaddingOnly_ReturnsEmpty()
    {
        Assert.Empty(Base32.Decode("===="));
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Base32.Decode("MZ8W6"));
    }

    // ── TOTP-relevant: Secret key decoding ─────────────────────────────

    [Fact]
    public void Decode_TotpTestSecret_ProducesExpectedBytes()
    {
        // "12345678901234567890" encoded in Base32 is "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ".
        string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        byte[] expected = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");

        byte[] result = Base32.Decode(base32Secret);

        Assert.Equal(expected, result);
    }
}
