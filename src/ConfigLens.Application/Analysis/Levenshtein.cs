namespace ConfigLens.Application.Analysis;

/// <summary>
/// Case-insensitive Levenshtein edit distance, used by the typo-suspicion rule
/// to find near-matches between keys read in code and keys in configuration.
/// </summary>
public static class Levenshtein
{
    /// <summary>Computes the edit distance between two strings, ignoring case.</summary>
    /// <param name="left">First string.</param>
    /// <param name="right">Second string.</param>
    public static int Distance(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var a = left.ToUpperInvariant();
        var b = right.ToUpperInvariant();

        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        // Two-row dynamic programming over the classic edit distance matrix.
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitutionCost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
