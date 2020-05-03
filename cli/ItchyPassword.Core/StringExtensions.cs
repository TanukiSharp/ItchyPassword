using System;

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
    }
}
