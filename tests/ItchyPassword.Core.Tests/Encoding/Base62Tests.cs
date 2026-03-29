using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Core.Tests.Encoding;

/// <summary>
/// Tests for <see cref="Base62"/> legacy decoding.
/// Base62 encoding is intentionally not supported (throws <see cref="NotSupportedException"/>).
/// </summary>
public sealed class Base62Tests
{
    // ── Encode is not supported ────────────────────────────────────────

    [Fact]
    public void Encode_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => Base62.Encode([1, 2, 3]));
    }

    [Fact]
    public void Encode_EmptyInput_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => Base62.Encode([]));
    }

    // ── Decode edge cases ──────────────────────────────────────────────

    [Fact]
    public void Decode_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(Base62.Decode(string.Empty));
    }

    [Fact]
    public void Decode_NullInput_ReturnsEmpty()
    {
        Assert.Empty(Base62.Decode(null!));
    }

    [Fact]
    public void Decode_WhitespaceInput_ReturnsEmpty()
    {
        Assert.Empty(Base62.Decode("   "));
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsArgumentException()
    {
        // '!' is not in the base62 alphabet.
        Assert.Throws<ArgumentException>(() => Base62.Decode("abc!def"));
    }

    // ── Decode regression: known legacy value ──────────────────────────

    [Fact]
    public void Decode_KnownValue_FirstByteIs0x61()
    {
        // "tfQA" decodes using the headered buffer format (2-byte LE length prefix).
        // The first data byte is 0x61 (ASCII 'a').
        byte[] result = Base62.Decode("tfQA");
        Assert.True(result.Length > 0);
        Assert.Equal(0x61, result[0]);
    }

    [Fact]
    public void Decode_Deterministic_SameInputSameOutput()
    {
        byte[] first = Base62.Decode("tfQA");
        byte[] second = Base62.Decode("tfQA");
        Assert.Equal(first, second);
    }

    // ── Alphabet verification ──────────────────────────────────────────

    [Fact]
    public void Alphabet_Contains62Characters()
    {
        Assert.Equal(62, Base62.Alphabet.Length);
    }

    [Fact]
    public void Alphabet_HasNoDuplicates()
    {
        Assert.Equal(Base62.Alphabet.Length, Base62.Alphabet.Distinct().Count());
    }
}
