using System.Numerics;

namespace ItchyPassword.Core.Encoding;

public static class Base62
{
    public const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Encode(byte[] input)
    {
        throw new NotSupportedException("Base62 encoding is not supported anymore.");

        /*
        if (input is null || input.Length == 0)
        {
            return string.Empty;
        }
        if (input.Length > 0xFFFF)
        {
            throw new ArgumentException("Buffer too large.");
        }

        // Create headered buffer (2 bytes LE length + data), matching TS createHeaderedBuffer.
        var headeredBuffer = new byte[2 + input.Length];
        headeredBuffer[0] = (byte)(input.Length % 256);
        headeredBuffer[1] = (byte)(input.Length / 256);
        Array.Copy(input, 0, headeredBuffer, 2, input.Length);

        // Convert to unsigned BigInteger (Little Endian), matching TS arrayBufferToUnsignedBigInt.
        var number = new BigInteger(headeredBuffer, isUnsigned: true, isBigEndian: false);

        // Divmod loop appending chars LSB first, matching TS toCustomBase.
        BigInteger alphabetLength = Alphabet.Length;
        var result = new StringBuilder();

        while (number > 0)
        {
            number = BigInteger.DivRem(number, alphabetLength, out BigInteger remainder);
            result.Append(Alphabet[(int)remainder]);
        }

        return result.ToString();
        */
    }

    public static byte[] Decode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        // Reconstruct BigInteger using multiplier (Little Endian), matching TS fromCustomBase.
        BigInteger alphabetLength = Alphabet.Length;
        BigInteger number = 0;
        BigInteger multiplier = 1;

        for (int i = 0; i < input.Length; i++)
        {
            int value = Alphabet.IndexOf(input[i]);
            if (value == -1)
            {
                throw new ArgumentException($"Invalid character '{input[i]}' in input string.");
            }
            number += value * multiplier;
            multiplier *= alphabetLength;
        }

        // Convert BigInteger to bytes (Little Endian), matching TS unsignedBigIntToArrayBuffer.
        List<byte> bytes = [];

        while (number > 0)
        {
            bytes.Add((byte)(number % 256));
            number /= 256;
        }

        // Parse the 2-byte LE length header.
        int totalLength = bytes.Count > 0 ? bytes[0] : 0;
        if (bytes.Count > 1)
        {
            totalLength += bytes[1] * 256;
        }

        // Pad with trailing zeros to match the declared length, matching TS logic.
        int diff = totalLength - (bytes.Count - 2);
        for (int i = 0; i < diff; i++)
        {
            bytes.Add(0);
        }

        // Return payload past the 2-byte header.
        return bytes.Skip(2).Take(totalLength).ToArray();
    }
}
