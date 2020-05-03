using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ItchyPassword.Core
{
    /// <summary>
    /// Contains string convertion extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates a string to the length. Remains untouched it input string it already smaller.
        /// </summary>
        /// <param name="input">Input string to truncate.</param>
        /// <param name="length">The maximum length to constraint the input string to.</param>
        /// <returns>Returns the input string, truncated or unmodified.</returns>
        public static string Truncate(this string input, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"Argument '{nameof(length)}' must be greater than or equal to zero.");

            if (input.Length < length)
                return input;

            return input.Substring(0, length);
        }

        /// <summary>
        /// Converts an hexadecimal encoded string to an array of bytes.
        /// </summary>
        /// <param name="input">The string containing hexadecimal encoded value to decode.</param>
        /// <returns>Returns the decoded array of bytes.</returns>
        public static byte[] FromBase16(this string input)
        {
            if (input.Length % 2 != 0)
                input = $"0{input}";

            byte[] result = new byte[input.Length / 2];

            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);

            return result;
        }

        private static byte[] BigIntegerToArrayBuffer(BigInteger number)
        {
            var result = new List<byte>();

            while (number > 0)
            {
                BigInteger remainder = number % 256;
                number /= 256;

                byte byteValue = (byte)remainder;

                result.Add(byteValue);
            }

            int totalLength = result[0];
            if (result.Count > 1) // For case where original buffer is of length 1 and contains 0.
                totalLength += result[1] * 256;

            // The varable 'result' contains 2 bytes of size header.
            int diff = totalLength - (result.Count - 2);

            for (int i = 0; i < diff; i++)
                result.Add(0);

            return result.Skip(2).ToArray();
        }

        /// <summary>
        /// Converts a string encoded in arbitrary base to an array of bytes.
        /// </summary>
        /// <param name="input">The string containing arbitrary base encoded value to decode.</param>
        /// <param name="alphabet">The alphabet used to encode the string.</param>
        /// <returns>rns the decoded array of bytes.</returns>
        public static byte[] FromCustomBase(this string input, string alphabet)
        {
            var alphabetLength = new BigInteger(alphabet.Length);

            BigInteger number = BigInteger.Zero;
            BigInteger multiplier = BigInteger.One;

            for (int i = 0; i < input.Length; i++)
            {
                var value = new BigInteger(alphabet.IndexOf(input[i]));

                number += value * multiplier;
                multiplier *= alphabetLength;
            }

            return BigIntegerToArrayBuffer(number);
        }
    }
}
