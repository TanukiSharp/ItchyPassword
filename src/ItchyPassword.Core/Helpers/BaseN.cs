using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ItchyPassword.Core.Helpers;

public static class BaseN
{
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
    /// Encodes byte array to string using a custom alphabet.
    /// Uses Little Endian interpretation of bytes and an optimized in-place division
    /// algorithm to avoid BigInteger overhead.
    /// This matches the TypeScript 'toCustomBaseOneWay' implementation.
    /// </summary>
    public static string EncodeOneWay(byte[] input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet)) throw new ArgumentException("Alphabet cannot be empty");
        if (alphabet.Length < 2) throw new ArgumentException("Alphabet length must be at least 2");
        if (input == null || input.Length == 0) return "";

        int baseValue = alphabet.Length;

        // Work on a copy since we modify the array in-place for division
        // We add an extra byte for padding if needed, although for division logic simple length is enough.
        // The simple specialized division algorithm doesn't need strict BigInteger compliance.
        byte[] buffer = new byte[input.Length];
        Array.Copy(input, buffer, input.Length);

        // Calculate approximate output capacity to avoid resizing
        // log_N(256^L) = L * log_N(256) = L * (8 / log_2(N))
        // approx factor = 8 / log2(alphabet.Length)
        int capacity = (int)(input.Length * 1.38); // 1.38 is adequate for Base58, larger for smaller bases
        if (baseValue < 58) capacity = input.Length * 8; // generic worst case for small alphabet

        var result = new StringBuilder(capacity);

        // We process the buffer until it's all zeros
        int length = buffer.Length;

        while (length > 0)
        {
            int remainder = 0;

            // Perform long division: Number = Number / Base
            // We iterate from MSB (end of array in little-endian) down to LSB (start)
            // But wait, the previous BigInteger logic treated input as Little Endian.
            // input[0] is LSB.
            // Standard long division goes from MSB to LSB.
            // MSB is at index length-1.

            for (int i = length - 1; i >= 0; i--)
            {
                int current = (remainder * 256) + buffer[i];
                buffer[i] = (byte)(current / baseValue);
                remainder = current % baseValue;
            }

            // Append the remainder (which is Number % Base)
            result.Append(alphabet[remainder]);

            // Optimization: Decrease the effective length of the buffer if the MSB becomes zero
            while (length > 0 && buffer[length - 1] == 0)
            {
                length--;
            }
        }

        return result.ToString();
    }

    public static byte[] Decode(string input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet)) throw new ArgumentException("Alphabet cannot be empty");
        if (alphabet.Length < 2) throw new ArgumentException("Alphabet length must be at least 2");
        if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

        // Based on fromCustomBaseFast (Big Endian logic matching Encode)
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
                bytes[j] &= 0xFF; // Keep only byte part
            }

            while (carry > 0)
            {
                bytes.Add(carry & 0xFF);
                carry >>= 8;
            }
        }

        // Handle leading zeros (represented by leading alphabet[0] chars)
        // Note: Encode includes leading zeros for input[i]==0 if it was BigEndian?
        // Actually Encode handles leading zeros manually.
        // But Decode needs to handle it if we want roundtrip.
        // The previous implementation had this loop.

        for (int i = 0; i < input.Length - 1 && input[i] == alphabet[0]; i++)
        {
            bytes.Add(0);
        }

        bytes.Reverse();
        return bytes.Select(b => (byte)b).ToArray();
    }
}
