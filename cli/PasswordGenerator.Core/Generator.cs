using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGenerator.Core
{
    /// <summary>
    /// Generates a derived password based on a master password.
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// The default and recommended amount of iterations used with the PBKDF2 algortihm.
        /// </summary>
        public const int DefaultIterations = 100_000;

        /// <summary>
        /// The default and recommended hash algorithm used with the PBKDF2 algortihm.
        /// </summary>
        public static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA512;

        /// <summary>
        /// Generates a derived password based on a master password and a public part, using the PBKDF2 algorithm.
        /// </summary>
        /// <param name="privateKey">The master password to derive a private key from.</param>
        /// <param name="publicKey">The public part, also called salt, used in the private key derivation function. When the public part converted to UTF8 is less than 8 bytes long, it is repeated until it is 8 bytes long.</param>
        /// <param name="iterations">The amount of iterations used with the PBKDF2 algortihm.</param>
        /// <param name="hashAlgorithm">The hash algorithm used with the PBKDF2 algortihm.</param>
        /// <returns>Returns a derived private key.</returns>
        public static byte[] GeneratePassword(string privateKey, string publicKey, int iterations, HashAlgorithmName hashAlgorithm)
        {
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            if (publicKey.Length == 0)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. Must not be empty.", nameof(publicKey));
            if (privateKey.Length == 0)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. Must not be empty.", nameof(publicKey));

            byte[] password = Encoding.UTF8.GetBytes(privateKey);
            byte[] salt = Encoding.UTF8.GetBytes(publicKey);

            if (salt.Length < 8)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. It produced {salt.Length} bytes, whereas 8 bytes are mandatory.", nameof(publicKey));

            using var algorithm = new Rfc2898DeriveBytes(password, salt, iterations, hashAlgorithm);

            return algorithm.GetBytes(32);
        }
    }
}
