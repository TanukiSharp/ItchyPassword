using System.Text;

namespace ItchyPassword.Core.Encoding;

public static class BaseN
{
    public static string Encode(byte[] input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet))
        {
            throw new ArgumentException("Alphabet cannot be empty.");
        }
        if (alphabet.Length < 2)
        {
            throw new ArgumentException("Alphabet length must be at least 2.");
        }
        if (input is null || input.Length == 0)
        {
            return string.Empty;
        }

        // Based on toCustomBaseFast from arrayUtils.ts
        List<int> digits = [0];

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

        // Handle leading zeros.
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
        if (string.IsNullOrEmpty(alphabet))
        {
            throw new ArgumentException("Alphabet cannot be empty.");
        }
        if (alphabet.Length < 2)
        {
            throw new ArgumentException("Alphabet length must be at least 2.");
        }
        if (input is null || input.Length == 0)
        {
            return string.Empty;
        }

        int baseValue = alphabet.Length;

        byte[] buffer = new byte[input.Length];
        Array.Copy(input, buffer, input.Length);

        // Calculate approximate output capacity to avoid resizing
        // log_N(256^L) = L * log_N(256) = L * (8 / log_2(N))
        // approx factor = 8 / log2(alphabet.Length)
        int capacity = (int)(input.Length * 1.38); // 1.38 is adequate for Base58, larger for smaller bases.
        if (baseValue < 58)
        {
            capacity = input.Length * 8; // generic worst case for small alphabet.
        }

        var result = new StringBuilder(capacity);

        int length = buffer.Length;

        while (length > 0)
        {
            int remainder = 0;

            for (int i = length - 1; i >= 0; i--)
            {
                int current = (remainder * 256) + buffer[i];
                buffer[i] = (byte)(current / baseValue);
                remainder = current % baseValue;
            }

            result.Append(alphabet[remainder]);

            while (length > 0 && buffer[length - 1] == 0)
            {
                length--;
            }
        }

        return result.ToString();
    }

    public static byte[] Decode(string input, string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet))
        {
            throw new ArgumentException("Alphabet cannot be empty.");
        }
        if (alphabet.Length < 2)
        {
            throw new ArgumentException("Alphabet length must be at least 2.");
        }
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        List<int> bytes = [0];

        foreach (char c in input)
        {
            int val = alphabet.IndexOf(c);

            if (val < 0)
            {
                throw new FormatException($"Character '{c}' not found in alphabet.");
            }

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

        for (int i = 0; i < input.Length - 1 && input[i] == alphabet[0]; i++)
        {
            bytes.Add(0);
        }

        bytes.Reverse();

        return bytes.Select(b => (byte)b).ToArray();
    }
}
