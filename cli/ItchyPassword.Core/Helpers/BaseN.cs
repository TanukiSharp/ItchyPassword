using System;
using System.Collections.Generic;
using System.Linq;

namespace ItchyPassword.Core.Helpers;

/// <summary>
/// Helper class for base-N encoding and decoding.
/// </summary>
public static class BaseN
{
    /// <summary>
    /// Encodes a byte array to a string using the specified alphabet.
    /// </summary>
    /// <param name="input">The byte array to encode.</param>
    /// <param name="alphabet">The alphabet to use for encoding.</param>
    /// <returns>The encoded string.</returns>
    public static string Encode(byte[] input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet)) throw new ArgumentException("Alphabet cannot be empty");
        if (alphabet.Length < 2) throw new ArgumentException("Alphabet length must be at least 2");

        if (input == null || input.Length == 0) return "";


        // Based on toCustomBaseFast from arrayUtils.ts
        var digits = new List<int> { 0 };

        for (int i = 0; i < input.Length; i++)
        {
            for (int j = 0; j < digits.Count; j++)
            {
                digits[j] <<= 8;
            }

            digits[0] += input[i];

            int carry = 0;
            for (int j = 0; j < digits.Count; j++)
            {
                digits[j] += carry;
                carry = digits[j] / alphabet.Length;
                digits[j] %= alphabet.Length;
            }

            while (carry > 0)
            {
                digits.Add(carry % alphabet.Length);
                carry /= alphabet.Length;
            }
        }

        // Handle leading zeros
        for (int i = 0; i < input.Length - 1 && input[i] == 0; i++)
        {
            digits.Add(0);
        }

        var result = new char[digits.Count];
        for (int i = 0; i < digits.Count; i++)
        {
            result[i] = alphabet[digits[digits.Count - 1 - i]];
        }

        return new string(result);
    }

    /// <summary>
    /// Decodes a string to a byte array using the specified alphabet.
    /// </summary>
    /// <param name="input">The string to decode.</param>
    /// <param name="alphabet">The alphabet used for encoding.</param>
    /// <returns>The decoded byte array.</returns>
    public static byte[] Decode(string input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet)) throw new ArgumentException("Alphabet cannot be empty");
        if (alphabet.Length < 2) throw new ArgumentException("Alphabet length must be at least 2");
        if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

        // Based on fromCustomBaseFast
        var bytes = new List<int> { 0 };

        foreach (char c in input)
        {
            int val = alphabet.IndexOf(c);
            if (val == -1) throw new FormatException($"Character '{c}' not found in alphabet.");

            for (int j = 0; j < bytes.Count; j++)
            {
                bytes[j] *= alphabet.Length;
            }

            bytes[0] += val;

            int carry = 0;
            for (int j = 0; j < bytes.Count; j++)
            {
                bytes[j] += carry;
                carry = bytes[j] >> 8;
                bytes[j] &= 0xFF;
            }

            while (carry > 0)
            {
                bytes.Add(carry & 0xFF);
                carry >>= 8;
            }
        }

        // Handle leading zeros (represented by leading alphabet[0] chars)
        for (int i = 0; i < input.Length - 1 && input[i] == alphabet[0]; i++)
        {
            bytes.Add(0); // This means leading zeros in output bytes
        }

        // Reverse to match correct byte order (big-endian/little-endian consistency)
        // TS implementation reverses bytes at the end
        bytes.Reverse();

        return bytes.Select(b => (byte)b).ToArray();
    }
}
