using System;
using System.Linq;

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
    /// </summary>
    public static byte[] Decode(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
        {
            return [];
        }

        // Strip whitespace and padding, then decode each character to its 5-bit value.
        byte[] fiveBitValues = base32
            .Where(c => char.IsWhiteSpace(c) == false && c != '=')
            .Select(DecodeChar)
            .ToArray();

        // Expand 5-bit values into a bit array.
        bool[] bits = new bool[fiveBitValues.Length * 5];
        int bitIndex = 0;

        foreach (byte value in fiveBitValues)
        {
            for (int i = 4; i >= 0; i--)
            {
                bits[bitIndex++] = (value & (1 << i)) != 0;
            }
        }

        // Pack bits back into bytes (8 bits each).
        int byteCount = bits.Length / 8;
        byte[] result = new byte[byteCount];
        int resultIndex = 0;
        byte currentByte = 0;
        int k = 0;

        for (int i = 0; i < byteCount * 8; i++)
        {
            if (bits[i])
            {
                currentByte |= (byte)(1 << (7 - k));
            }

            k++;

            if (k >= 8)
            {
                result[resultIndex++] = currentByte;
                currentByte = 0;
                k = 0;
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
