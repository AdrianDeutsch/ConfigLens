namespace ConfigLens.Application.Analysis;

/// <summary>
/// Shannon entropy of a string in bits per character. Used by the secrets rule
/// to distinguish random-looking tokens from ordinary configuration values.
/// </summary>
public static class ShannonEntropy
{
    /// <summary>
    /// Computes the Shannon entropy of <paramref name="value"/> in bits per character.
    /// Returns 0 for empty and single-character strings; the theoretical maximum
    /// for a string of length n is log2(n).
    /// </summary>
    /// <param name="value">The string to analyze.</param>
    public static double OfString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length <= 1)
        {
            return 0;
        }

        var counts = new Dictionary<char, int>();
        foreach (var ch in value)
        {
            counts[ch] = counts.TryGetValue(ch, out var count) ? count + 1 : 1;
        }

        double entropy = 0;
        foreach (var count in counts.Values)
        {
            var probability = (double)count / value.Length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }
}
