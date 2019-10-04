using System;

namespace PasswordGenerator.Core
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
    }
}
