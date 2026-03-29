using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Core.Tests.Encoding;

/// <summary>
/// Tests for <see cref="BaseN"/> encoding and decoding with hardcoded regression values.
/// </summary>
public sealed class BaseNTests
{
    private const string HexAlphabet = "0123456789abcdef";
    private const string BinaryAlphabet = "01";
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    // ── Encode regression ──────────────────────────────────────────────

    [Fact]
    public void Encode_DeadBeef_HexAlphabet_ProducesExpectedOutput()
    {
        byte[] input = [0xDE, 0xAD, 0xBE, 0xEF];
        string result = BaseN.Encode(input, HexAlphabet);
        Assert.Equal("deadbeef", result);
    }

    [Fact]
    public void Encode_SingleByte42_BinaryAlphabet_ProducesExpectedOutput()
    {
        byte[] input = [42];
        string result = BaseN.Encode(input, BinaryAlphabet);
        Assert.Equal("101010", result);
    }

    [Fact]
    public void Encode_EmptyInput_ReturnsEmpty()
    {
        string result = BaseN.Encode([], HexAlphabet);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encode_NullInput_ReturnsEmpty()
    {
        string result = BaseN.Encode(null!, HexAlphabet);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encode_LeadingZeros_PreservesLeadingZeros()
    {
        byte[] input = [0, 0, 1, 2, 3];
        string result = BaseN.Encode(input, Base58Alphabet);
        // Leading zeros map to the first character of the alphabet.
        Assert.StartsWith("11", result);
        Assert.Equal("11Ldp", result);
    }

    [Fact]
    public void Encode_SingleZero_MapsToFirstAlphabetChar()
    {
        byte[] input = [0];
        string result = BaseN.Encode(input, Base58Alphabet);
        Assert.Equal("1", result);
    }

    // ── Decode regression ──────────────────────────────────────────────

    [Fact]
    public void Decode_DeadBeef_HexAlphabet_ReturnsExpectedBytes()
    {
        byte[] result = BaseN.Decode("deadbeef", HexAlphabet);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, result);
    }

    [Fact]
    public void Decode_EmptyInput_ReturnsEmpty()
    {
        byte[] result = BaseN.Decode(string.Empty, HexAlphabet);
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_NullInput_ReturnsEmpty()
    {
        byte[] result = BaseN.Decode(null!, HexAlphabet);
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => BaseN.Decode("xyz", HexAlphabet));
    }

    [Fact]
    public void Decode_LeadingAlphabetZeros_PreservesLeadingZeros()
    {
        byte[] result = BaseN.Decode("11Ldp", Base58Alphabet);
        Assert.Equal(new byte[] { 0, 0, 1, 2, 3 }, result);
    }

    // ── Encode/Decode round-trip ───────────────────────────────────────

    [Theory]
    [InlineData(new byte[] { 1 })]
    [InlineData(new byte[] { 255 })]
    [InlineData(new byte[] { 0, 1, 2, 3, 4, 5 })]
    [InlineData(new byte[] { 0, 0, 0, 42 })]
    [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 })]
    public void RoundTrip_EncodeDecodeBase58_PreservesBytes(byte[] input)
    {
        string encoded = BaseN.Encode(input, Base58Alphabet);
        byte[] decoded = BaseN.Decode(encoded, Base58Alphabet);
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void RoundTrip_EncodeDecodeHex_PreservesBytes()
    {
        byte[] input = [0xCA, 0xFE, 0xBA, 0xBE];
        string encoded = BaseN.Encode(input, HexAlphabet);
        byte[] decoded = BaseN.Decode(encoded, HexAlphabet);
        Assert.Equal(input, decoded);
    }

    // ── EncodeOneWay regression ────────────────────────────────────────

    [Fact]
    public void EncodeOneWay_DeadBeef_HexAlphabet_ProducesExpectedOutput()
    {
        byte[] input = [0xDE, 0xAD, 0xBE, 0xEF];
        string result = BaseN.EncodeOneWay(input, HexAlphabet);
        Assert.Equal("eddaebfe", result);
    }

    [Fact]
    public void EncodeOneWay_DeadBeef_Base58Alphabet_ProducesExpectedOutput()
    {
        byte[] input = [0xDE, 0xAD, 0xBE, 0xEF];
        string result = BaseN.EncodeOneWay(input, Base58Alphabet);
        Assert.Equal("j56S87", result);
    }

    [Fact]
    public void EncodeOneWay_EmptyInput_ReturnsEmpty()
    {
        string result = BaseN.EncodeOneWay([], HexAlphabet);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EncodeOneWay_NullInput_ReturnsEmpty()
    {
        string result = BaseN.EncodeOneWay(null!, HexAlphabet);
        Assert.Equal(string.Empty, result);
    }

    // ── EncodeOneWay is NOT the same as Encode (different byte order) ──

    [Fact]
    public void EncodeOneWay_DiffersFromEncode()
    {
        byte[] input = [0xDE, 0xAD, 0xBE, 0xEF];

        string encode = BaseN.Encode(input, HexAlphabet);
        string oneWay = BaseN.EncodeOneWay(input, HexAlphabet);

        Assert.Equal("deadbeef", encode);
        Assert.Equal("eddaebfe", oneWay);
        Assert.NotEqual(encode, oneWay);
    }

    // ── Validation ─────────────────────────────────────────────────────

    [Fact]
    public void Encode_EmptyAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.Encode([1], string.Empty));
    }

    [Fact]
    public void Encode_SingleCharAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.Encode([1], "a"));
    }

    [Fact]
    public void Decode_EmptyAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.Decode("abc", string.Empty));
    }

    [Fact]
    public void Decode_SingleCharAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.Decode("a", "a"));
    }

    [Fact]
    public void EncodeOneWay_EmptyAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.EncodeOneWay([1], string.Empty));
    }

    [Fact]
    public void EncodeOneWay_SingleCharAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => BaseN.EncodeOneWay([1], "a"));
    }
}
