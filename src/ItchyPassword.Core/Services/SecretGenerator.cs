namespace ItchyPassword.Core.Services;

/// <summary>
/// Rules for generating a random secret with character class constraints.
/// </summary>
public class SecretGenerationRules
{
    public const string DefaultSymbolAlphabet = "!@#$%^&*()-_=+[]{}|;:',.<>?/`~";

    public int TotalLength { get; set; } = 64;
    public int MinLowercase { get; set; } = 3;
    public int MinUppercase { get; set; } = 3;
    public int MinDigits { get; set; } = 3;
    public int MinSymbols { get; set; } = 3;
    public string SymbolAlphabet { get; set; } = DefaultSymbolAlphabet;

    /// <summary>
    /// Returns the sum of all minimum character class requirements.
    /// </summary>
    public int MinimumRequired
    {
        get
        {
            return MinLowercase + MinUppercase + MinDigits + MinSymbols;
        }
    }

    /// <summary>
    /// Validates that the rules are internally consistent.
    /// </summary>
    public bool IsValid(out string? error)
    {
        if (TotalLength < 1)
        {
            error = "Total length must be at least 1.";
            return false;
        }

        if (MinLowercase < 0 || MinUppercase < 0 || MinDigits < 0 || MinSymbols < 0)
        {
            error = "Minimum counts cannot be negative.";
            return false;
        }

        if (MinimumRequired > TotalLength)
        {
            error = $"Sum of minimums ({MinimumRequired}) exceeds total length ({TotalLength}).";
            return false;
        }

        if (MinSymbols > 0 && string.IsNullOrEmpty(SymbolAlphabet))
        {
            error = "Symbol alphabet cannot be empty when minimum symbols is greater than 0.";
            return false;
        }

        // When the total length exceeds the sum of minimums, remaining positions are filled
        // from a combined alphabet built only from classes with min > 0. At least one class
        // must have min > 0 so the combined alphabet is not empty.
        if (MinimumRequired < TotalLength && MinLowercase == 0 && MinUppercase == 0 && MinDigits == 0 && MinSymbols == 0)
        {
            error = "At least one character class must have a minimum greater than 0.";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>
/// Generates random secrets based on character class rules, using externally provided random bytes.
/// </summary>
public static class SecretGenerator
{
    public const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    public const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string DigitChars = "0123456789";

    /// <summary>
    /// Calculates the number of random bytes needed to generate a secret with the given rules.
    /// Each character pick uses 2 bytes, and the Fisher-Yates shuffle uses 2 bytes per swap.
    /// </summary>
    public static int CalculateRequiredRandomBytes(int totalLength)
    {
        // 2 bytes per character selection + 2 bytes per shuffle swap + safety margin.
        return (totalLength * 4) + 64;
    }

    /// <summary>
    /// Generates a random secret string that satisfies the given rules.
    /// </summary>
    /// <param name="rules">The generation rules specifying character class minimums.</param>
    /// <param name="randomBytes">Cryptographically secure random bytes to use as entropy source.</param>
    /// <returns>A randomly generated secret string.</returns>
    /// <exception cref="ArgumentException">When the rules are invalid or insufficient random bytes are provided.</exception>
    public static string Generate(SecretGenerationRules rules, byte[] randomBytes)
    {
        if (rules.IsValid(out string? error) == false)
        {
            throw new ArgumentException(error);
        }

        int requiredBytes = CalculateRequiredRandomBytes(rules.TotalLength);

        if (randomBytes.Length < requiredBytes)
        {
            throw new ArgumentException(
                $"Insufficient random bytes. Need at least {requiredBytes}, got {randomBytes.Length}."
            );
        }

        var result = new char[rules.TotalLength];
        int byteIndex = 0;
        int charIndex = 0;

        // Fill minimum lowercase characters.
        for (int i = 0; i < rules.MinLowercase; i++)
        {
            result[charIndex++] = PickChar(LowercaseChars, randomBytes, ref byteIndex);
        }

        // Fill minimum uppercase characters.
        for (int i = 0; i < rules.MinUppercase; i++)
        {
            result[charIndex++] = PickChar(UppercaseChars, randomBytes, ref byteIndex);
        }

        // Fill minimum digit characters.
        for (int i = 0; i < rules.MinDigits; i++)
        {
            result[charIndex++] = PickChar(DigitChars, randomBytes, ref byteIndex);
        }

        // Fill minimum symbol characters.
        for (int i = 0; i < rules.MinSymbols; i++)
        {
            result[charIndex++] = PickChar(rules.SymbolAlphabet, randomBytes, ref byteIndex);
        }

        // Build combined alphabet for remaining positions.
        string allChars = BuildCombinedAlphabet(rules);

        // Fill remaining positions with random characters from the combined alphabet.
        while (charIndex < rules.TotalLength)
        {
            result[charIndex++] = PickChar(allChars, randomBytes, ref byteIndex);
        }

        // Fisher-Yates shuffle to randomize character positions.
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = GetBoundedRandomIndex(i + 1, randomBytes, ref byteIndex);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return new string(result);
    }

    /// <summary>
    /// Builds the combined alphabet from only the character classes that have a minimum greater than zero.
    /// When a character class minimum is zero, those characters are excluded entirely from the output.
    /// </summary>
    private static string BuildCombinedAlphabet(SecretGenerationRules rules)
    {
        string combined = string.Empty;

        if (rules.MinLowercase > 0)
        {
            combined += LowercaseChars;
        }

        if (rules.MinUppercase > 0)
        {
            combined += UppercaseChars;
        }

        if (rules.MinDigits > 0)
        {
            combined += DigitChars;
        }

        if (rules.MinSymbols > 0 && string.IsNullOrEmpty(rules.SymbolAlphabet) == false)
        {
            combined += rules.SymbolAlphabet;
        }

        return combined;
    }

    private static char PickChar(string alphabet, byte[] randomBytes, ref int byteIndex)
    {
        int value = ReadTwoBytes(randomBytes, ref byteIndex);
        return alphabet[value % alphabet.Length];
    }

    private static int GetBoundedRandomIndex(int maxExclusive, byte[] randomBytes, ref int byteIndex)
    {
        int value = ReadTwoBytes(randomBytes, ref byteIndex);
        return value % maxExclusive;
    }

    /// <summary>
    /// Reads two bytes from the random byte array and returns a 16-bit unsigned value.
    /// </summary>
    private static int ReadTwoBytes(byte[] randomBytes, ref int byteIndex)
    {
        int value = (randomBytes[byteIndex] << 8) | randomBytes[byteIndex + 1];
        byteIndex += 2;
        return value;
    }
}
