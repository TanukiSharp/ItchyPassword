using System.Collections.Frozen;
using System.Collections.Immutable;

namespace ItchyPassword.Core.Services;

/// <summary>
/// Validates alphabets used for password generation.
/// Detects duplicate characters that would reduce entropy.
/// </summary>
public static class AlphabetValidator
{
    /// <summary>
    /// Finds duplicate characters in the given <paramref name="alphabet"/>.
    /// </summary>
    /// <returns>
    /// An immutable dictionary where keys are duplicate characters and values are their occurrence counts (≥ 2).
    /// Empty if no duplicates are found.
    /// </returns>
    public static FrozenDictionary<char, int> FindDuplicates(string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet))
        {
            return FrozenDictionary<char, int>.Empty;
        }

        Dictionary<char, int> counts = [];

        foreach (char c in alphabet)
        {
            counts[c] = counts.GetValueOrDefault(c) + 1;
        }

        return counts
            .Where(kvp => kvp.Value >= 2)
            .ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Removes duplicate characters from the <paramref name="alphabet"/>, keeping the first occurrence of each.
    /// </summary>
    public static string Deduplicate(string alphabet)
    {
        if (string.IsNullOrEmpty(alphabet))
        {
            return string.Empty;
        }

        int index = 0;
        Span<char> result = stackalloc char[alphabet.Length];

        foreach (char c in alphabet)
        {
            if (result.Contains(c) == false)
            {
                result[index++] = c;
            }
        }

        return new string(result[..index]);
    }
}
