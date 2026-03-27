using System;

namespace ItchyPassword.Core.Encoding;

/// <summary>
/// RFC 4648 Base32 encoder and decoder.
/// Alphabet: A-Z, 2-7 (case-insensitive on decode).
/// </summary>
public static class Base32
{
    /// <summary>
    /// Decodes a Base32-encoded string into its original byte representation.
    /// Whitespace and padding ('=') characters are stripped before decoding.
    /// Uses a bit accumulator to convert 5-bit values directly into bytes
    /// without intermediate arrays.
    /// </summary>
    public static byte[] Decode(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
        {
            return [];
        }

        // Count valid characters to size the output buffer.
        int validCount = 0;

        foreach (char c in base32)
        {
            if (char.IsWhiteSpace(c) == false && c != '=')
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return [];
        }

        // Each valid character contributes 5 bits; output is groups of 8 bits.
        int byteCount = validCount * 5 / 8;
        byte[] result = new byte[byteCount];

        int buffer = 0;
        int bitsInBuffer = 0;
        int resultIndex = 0;

        foreach (char c in base32)
        {
            if (char.IsWhiteSpace(c) || c == '=')
            {
                continue;
            }

            buffer = (buffer << 5) | DecodeChar(c);
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                result[resultIndex++] = (byte)(buffer >> bitsInBuffer);
                buffer &= (1 << bitsInBuffer) - 1;
            }
        }

        return result;
    }

    /// <summary>
    /// Decodes a single Base32 character to its 5-bit numeric value.
    /// </summary>
    private static byte DecodeChar(char c)
    {
        if (c >= 'a' && c <= 'z')
        {
            return (byte)(c - 'a');
        }

        if (c >= 'A' && c <= 'Z')
        {
            return (byte)(c - 'A');
        }

        if (c >= '2' && c <= '7')
        {
            return (byte)(c - '2' + 26);
        }

        throw new FormatException($"Invalid Base32 character: '{c}'.");
    }
}
