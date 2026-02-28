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
/// Generates random secrets based on character class rules, using a <see cref="RandomBytePool"/>
/// for cryptographically secure randomness with automatic re-fetching on exhaustion.
/// </summary>
public static class SecretGenerator
{
    public const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    public const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string DigitChars = "0123456789";

    /// <summary>
    /// Generates a random secret string that satisfies the given rules.
    /// </summary>
    /// <param name="rules">The generation rules specifying character class minimums.</param>
    /// <param name="pool">The random byte pool used as entropy source.</param>
    /// <returns>A randomly generated secret string.</returns>
    /// <exception cref="ArgumentException">When the rules are invalid.</exception>
    public static async Task<string> GenerateAsync(SecretGenerationRules rules, RandomBytePool pool)
    {
        if (rules.IsValid(out string? error) == false)
        {
            throw new ArgumentException(error);
        }

        var result = new char[rules.TotalLength];
        int charIndex = 0;

        // Fill minimum lowercase characters.
        for (int i = 0; i < rules.MinLowercase; i++)
        {
            result[charIndex++] = await PickCharAsync(LowercaseChars, pool);
        }

        // Fill minimum uppercase characters.
        for (int i = 0; i < rules.MinUppercase; i++)
        {
            result[charIndex++] = await PickCharAsync(UppercaseChars, pool);
        }

        // Fill minimum digit characters.
        for (int i = 0; i < rules.MinDigits; i++)
        {
            result[charIndex++] = await PickCharAsync(DigitChars, pool);
        }

        // Fill minimum symbol characters.
        for (int i = 0; i < rules.MinSymbols; i++)
        {
            result[charIndex++] = await PickCharAsync(rules.SymbolAlphabet, pool);
        }

        // Build combined alphabet for remaining positions.
        string allChars = BuildCombinedAlphabet(rules);

        // Fill remaining positions with random characters from the combined alphabet.
        while (charIndex < rules.TotalLength)
        {
            result[charIndex++] = await PickCharAsync(allChars, pool);
        }

        // Fisher-Yates shuffle to randomize character positions.
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = await GetBoundedRandomIndexAsync(i + 1, pool);
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

    /// <summary>
    /// Picks a character from the alphabet using rejection sampling to eliminate modulo bias.
    /// </summary>
    private static async Task<char> PickCharAsync(string alphabet, RandomBytePool pool)
    {
        int index = await GetBoundedRandomIndexAsync(alphabet.Length, pool);
        return alphabet[index];
    }

    /// <summary>
    /// Returns a uniformly distributed random index in [0, maxExclusive) using rejection sampling.
    /// Reads 2 bytes at a time (16-bit value space) and rejects values that would cause modulo bias.
    /// Fetches a fresh batch of random bytes when the current one is exhausted.
    /// </summary>
    private static async Task<int> GetBoundedRandomIndexAsync(int maxExclusive, RandomBytePool pool)
    {
        // The largest multiple of maxExclusive that fits in a 16-bit value.
        // Values >= this threshold are rejected to eliminate modulo bias.
        int threshold = (65536 / maxExclusive) * maxExclusive;

        while (true)
        {
            int value = await pool.ReadTwoBytesAsync();

            if (value < threshold)
            {
                return value % maxExclusive;
            }
        }
    }
}
