using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Core.Tests.Encoding;

/// <summary>
/// Tests for <see cref="Base58"/> encoding/decoding with known regression values.
/// </summary>
public sealed class Base58Tests
{
    // ── Encode regression ──────────────────────────────────────────────

    [Fact]
    public void Encode_HelloWorld_ProducesExpectedOutput()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes("Hello World");
        Assert.Equal("JxF12TrwUP45BMd", Base58.Encode(input));
    }

    [Fact]
    public void Encode_SingleZeroByte_Returns1()
    {
        Assert.Equal("1", Base58.Encode([0]));
    }

    [Fact]
    public void Encode_TwoZeroBytes_Returns11()
    {
        Assert.Equal("11", Base58.Encode([0, 0]));
    }

    [Fact]
    public void Encode_MixedLeadingZeros_ProducesExpectedOutput()
    {
        Assert.Equal("11Ldp", Base58.Encode([0, 0, 1, 2, 3]));
    }

    [Fact]
    public void Encode_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Base58.Encode([]));
    }

    // ── Decode regression ──────────────────────────────────────────────

    [Fact]
    public void Decode_KnownEncoded_ReturnsOriginalBytes()
    {
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Hello World");
        Assert.Equal(expected, Base58.Decode("JxF12TrwUP45BMd"));
    }

    [Fact]
    public void Decode_1_ReturnsSingleZeroByte()
    {
        Assert.Equal(new byte[] { 0 }, Base58.Decode("1"));
    }

    [Fact]
    public void Decode_11_ReturnsTwoZeroBytes()
    {
        Assert.Equal(new byte[] { 0, 0 }, Base58.Decode("11"));
    }

    [Fact]
    public void Decode_MixedLeadingZeros_ReturnsExpectedBytes()
    {
        Assert.Equal(new byte[] { 0, 0, 1, 2, 3 }, Base58.Decode("11Ldp"));
    }

    [Fact]
    public void Decode_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(Base58.Decode(string.Empty));
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsFormatException()
    {
        // '0' (zero), 'O', 'I', 'l' are not in Base58 alphabet.
        Assert.Throws<FormatException>(() => Base58.Decode("0Invalid"));
        Assert.Throws<FormatException>(() => Base58.Decode("OInvalid"));
        Assert.Throws<FormatException>(() => Base58.Decode("IInvalid"));
        Assert.Throws<FormatException>(() => Base58.Decode("lInvalid"));
    }

    // ── Round-trip ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(new byte[] { 1 })]
    [InlineData(new byte[] { 0 })]
    [InlineData(new byte[] { 0, 0 })]
    [InlineData(new byte[] { 255, 255, 255 })]
    [InlineData(new byte[] { 0, 0, 0, 1 })]
    [InlineData(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE })]
    public void RoundTrip_EncodeDecode_PreservesBytes(byte[] input)
    {
        string encoded = Base58.Encode(input);
        byte[] decoded = Base58.Decode(encoded);
        Assert.Equal(input, decoded);
    }

    // ── Alphabet constraints ───────────────────────────────────────────

    [Fact]
    public void Encode_OutputContainsOnlyBase58Characters()
    {
        const string validChars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        byte[] input = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE];

        string encoded = Base58.Encode(input);

        foreach (char c in encoded)
        {
            Assert.Contains(c, validChars);
        }
    }
}
