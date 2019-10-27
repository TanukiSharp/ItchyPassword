using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace PasswordGenerator.Core
{
    /// <summary>
    /// Contains byte array convertion extension methods.
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Converts a large number stored as a byte array into a string, using an arbitrary base, described by a given alphabet.
        /// </summary>
        /// <param name="largeNumber">Large number, represented as byte array, to convert to sting, using a custom base.</param>
        /// <param name="alphabet">Description of the base to use. The size of the alphabet define the base.</param>
        /// <returns>Returns the large number converted to string.</returns>
        public static string ToCustomBase(this byte[] largeNumber, string alphabet)
        {
            if (largeNumber == null)
                throw new ArgumentNullException(nameof(largeNumber));
            if (alphabet == null)
                throw new ArgumentNullException(nameof(alphabet));

            if (alphabet.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(alphabet), alphabet, $"Argument '{nameof(alphabet)}' must be greater than or equal to 2.");

            var number = new BigInteger(largeNumber, true);
            var alphabetLength = new BigInteger(alphabet.Length);

            var result = new StringBuilder();

            while (number > BigInteger.Zero)
            {
                number = BigInteger.DivRem(number, alphabetLength, out BigInteger remainder);
                int index = int.Parse(remainder.ToString());

                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts the input byte array to base 64.
        /// </summary>
        /// <param name="bytes">Input to convert to base 64.</param>
        /// <returns>Returns the input converted to a base 64 string.</returns>
        public static string ToBase64(this byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Converts the input byte array to base 16.
        /// </summary>
        /// <param name="bytes">Input to convert to base 16.</param>
        /// <returns>Returns the input converted to a base 16 string.</returns>
        public static string ToBase16(this byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var sb = new StringBuilder();

            foreach (var x in bytes)
                sb.Append(x.ToString("x2"));

            return sb.ToString();
        }
    }
}
