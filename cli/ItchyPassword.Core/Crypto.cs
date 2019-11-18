using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ItchyPassword.Core
{
    /// <summary>
    /// Generates a derived password based on a master password.
    /// </summary>
    public static class Crypto
    {
        /// <summary>
        /// Generates a derived password based on a master password and a public part, using the HKDF algorithm (PBKDF2 and HMAC-SHA512).
        /// </summary>
        /// <param name="privateKey">The master password to derive a private key from.</param>
        /// <param name="publicKey">The public part, also called salt, used in the private key derivation function. When the public part converted to UTF8 is less than 8 bytes long, it is repeated until it is 8 bytes long.</param>
        /// <param name="hkdfPurpose">An additional purpose to derive key again through HMAC algorithm.</param>
        /// <returns>Returns a derived private key.</returns>
        public static byte[] GeneratePassword(byte[] privateKey, byte[] publicKey, string hkdfPurpose)
        {
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));

            if (privateKey.Length == 0)
                throw new ArgumentException($"Argument '{nameof(privateKey)}' is invalid. Must not be empty.", nameof(privateKey));
            if (publicKey.Length < 8)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. It must be at least 8 bytes long.", nameof(publicKey));

            using var algorithm = new Rfc2898DeriveBytes(privateKey, publicKey, 100_000, HashAlgorithmName.SHA512);

            using var hkdfAlgorithm = new HMACSHA512(algorithm.GetBytes(32));
            byte[] key = hkdfAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(hkdfPurpose));

            return key;
        }

        private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

        /// <summary>
        /// Length in bytes used for the nonce in AES-GCM algorithm.
        /// </summary>
        public const int NonceLength = 12;

        /// <summary>
        /// Length in bytes used for the authenticated tag in AES-GCM algorithm.
        /// </summary>
        public const int AuthenticatedTagLength = 16;

        /// <summary>
        /// Length in bytes used for the password hash salt.
        /// </summary>
        public const int PasswordHashSaltLength = 16;

        /// <summary>
        /// Encrypts data.
        /// </summary>
        /// <param name="input">Data to encrypt.</param>
        /// <param name="password">Encryption key.</param>
        /// <param name="output">Receives the nonce, auth tag and encrypted value. Must be at least input length + 44 bytes long.</param>
        /// <returns>Returns encrypted data.</returns>
        public static void Encrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> password, Span<byte> output)
        {
            int minimumOutputLength = input.Length + NonceLength + AuthenticatedTagLength + PasswordHashSaltLength;

            if (output.Length < minimumOutputLength)
                throw new ArgumentException($"Argument '{nameof(output)}' must be at least {minimumOutputLength} bytes long.", nameof(output));

            Span<byte> passwordSalt = output.Slice(NonceLength, PasswordHashSaltLength);
            random.GetBytes(passwordSalt);

            // The reason to derive key again is to ensure 32 bytes length.
            var keyDerivedBytes = new Rfc2898DeriveBytes(password.ToArray(), passwordSalt.ToArray(), 100_000, HashAlgorithmName.SHA512);

            using var aes = new AesGcm(keyDerivedBytes.GetBytes(32));

            Span<byte> nonce = output.Slice(0, NonceLength);
            random.GetBytes(nonce);

            aes.Encrypt(
                nonce,
                input,
                output.Slice(NonceLength + PasswordHashSaltLength, input.Length),
                output.Slice(NonceLength + PasswordHashSaltLength + input.Length, AuthenticatedTagLength)
            );
        }

        /// <summary>
        /// Encrypts data.
        /// </summary>
        /// <param name="input">Data to encrypt.</param>
        /// <param name="password">Encryption key.</param>
        /// <returns>Returns encrypted data.</returns>
        public static byte[] Encrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> password)
        {
            byte[] output = new byte[NonceLength + AuthenticatedTagLength + PasswordHashSaltLength + input.Length];

            Encrypt(input, password, output);

            return output;
        }

        /// <summary>
        /// Decrypts data.
        /// </summary>
        /// <param name="input">Data to decrypt.</param>
        /// <param name="password">Decryption key.</param>
        /// <param name="output">Receives the decrypted value. Must be at least input length minus 44 bytes long.</param>
        /// <returns>Returns decrypted data.</returns>
        public static void Decrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> password, Span<byte> output)
        {
            int payloadLength = input.Length - NonceLength - AuthenticatedTagLength - PasswordHashSaltLength;

            if (output.Length < payloadLength)
                throw new ArgumentException($"Argument '{nameof(output)}' must be at least {payloadLength} bytes long.", nameof(output));

            ReadOnlySpan<byte> passwordSalt = input.Slice(NonceLength, PasswordHashSaltLength);

            // The reason to derive key again is to ensure 32 bytes length.
            var keyDerivedBytes = new Rfc2898DeriveBytes(password.ToArray(), passwordSalt.ToArray(), 100_000, HashAlgorithmName.SHA512);

            using var aes = new AesGcm(keyDerivedBytes.GetBytes(32));

            aes.Decrypt(
                input.Slice(0, NonceLength),
                input.Slice(NonceLength + PasswordHashSaltLength, payloadLength),
                input.Slice(NonceLength + PasswordHashSaltLength + payloadLength, AuthenticatedTagLength),
                output
            );
        }

        /// <summary>
        /// Decrypts data.
        /// </summary>
        /// <param name="input">Data to decrypt.</param>
        /// <param name="password">Decryption key.</param>
        /// <returns>Returns decrypted data.</returns>
        public static byte[] Decrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> password)
        {
            byte[] output = new byte[input.Length - NonceLength - AuthenticatedTagLength - PasswordHashSaltLength];

            Decrypt(input, password, output);

            return output;
        }
    }
}
