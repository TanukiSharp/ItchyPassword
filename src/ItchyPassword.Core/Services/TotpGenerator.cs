using System;
using System.Security.Cryptography;
using ItchyPassword.Core.Encoding;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Generates Time-based One-Time Passwords (TOTP) per RFC 6238.
/// </summary>
public static class TotpGenerator
{
    private const int DefaultDigitCount = 6;
    private const int TimeStepSeconds = 30;
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Computes a TOTP code for the current UTC time.
    /// </summary>
    /// <param name="base32Secret">The shared secret encoded as a Base32 string.</param>
    /// <param name="digitCount">Number of digits in the output code (1–8, default 6).</param>
    /// <returns>The TOTP code, zero-padded to <paramref name="digitCount"/> digits.</returns>
    public static string GenerateCode(string base32Secret, int digitCount = DefaultDigitCount)
    {
        return GenerateCode(base32Secret, DateTime.UtcNow, digitCount);
    }

    /// <summary>
    /// Computes a TOTP code for a specific point in time.
    /// </summary>
    /// <param name="base32Secret">The shared secret encoded as a Base32 string.</param>
    /// <param name="timestamp">The UTC timestamp to compute the code for.</param>
    /// <param name="digitCount">Number of digits in the output code (1–8, default 6).</param>
    /// <returns>The TOTP code, zero-padded to <paramref name="digitCount"/> digits.</returns>
    public static string GenerateCode(string base32Secret, DateTime timestamp, int digitCount = DefaultDigitCount)
    {
        if (string.IsNullOrWhiteSpace(base32Secret))
        {
            throw new ArgumentException("Secret cannot be empty.", nameof(base32Secret));
        }

        if (digitCount < 1 || digitCount > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(digitCount), "Digit count must be between 1 and 8.");
        }

        byte[] secretBytes = Base32.Decode(base32Secret);
        ulong timeStep = GetTimeStep(timestamp);
        byte[] timeBytes = GetBigEndianBytes(timeStep);

        byte[] hash = HMACSHA1.HashData(secretBytes, timeBytes);

        int code = DynamicTruncate(hash);
        int modulo = (int)Math.Pow(10, digitCount);

        return (code % modulo).ToString().PadLeft(digitCount, '0');
    }

    /// <summary>
    /// Returns the number of whole seconds remaining in the current 30-second TOTP window.
    /// </summary>
    public static int GetRemainingSeconds()
    {
        return GetRemainingSeconds(DateTime.UtcNow);
    }

    /// <summary>
    /// Returns the number of whole seconds remaining in the TOTP window for the given timestamp.
    /// </summary>
    public static int GetRemainingSeconds(DateTime timestamp)
    {
        double elapsed = (timestamp - UnixEpoch).TotalSeconds % TimeStepSeconds;
        return TimeStepSeconds - (int)Math.Floor(elapsed);
    }

    private static ulong GetTimeStep(DateTime timestamp)
    {
        TimeSpan elapsed = timestamp - UnixEpoch;
        return (ulong)Math.Floor(elapsed.TotalSeconds / TimeStepSeconds);
    }

    /// <summary>
    /// Converts a 64-bit unsigned integer to an 8-byte big-endian array.
    /// </summary>
    private static byte[] GetBigEndianBytes(ulong value)
    {
        byte[] bytes = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    /// <summary>
    /// RFC 4226 §5.4 dynamic truncation.
    /// </summary>
    private static int DynamicTruncate(byte[] hash)
    {
        int offset = hash[^1] & 0x0F;

        int truncated = 0;

        for (int i = 0; i < 4; i++)
        {
            truncated <<= 8;
            truncated |= hash[offset + i];
        }

        truncated &= 0x7FFFFFFF;

        return truncated;
    }
}
