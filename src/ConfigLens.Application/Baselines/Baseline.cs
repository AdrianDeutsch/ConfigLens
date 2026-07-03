using ConfigLens.Domain;

namespace ConfigLens.Application.Baselines;

/// <summary>
/// A set of known finding fingerprints. Findings in the baseline are
/// suppressed so adopting ConfigLens in a legacy codebase starts clean and
/// only new findings fail the build (ADR-0006).
/// </summary>
public sealed class Baseline
{
    /// <summary>A baseline that suppresses nothing.</summary>
    public static readonly Baseline Empty = new([]);

    private readonly HashSet<string> _fingerprints;

    /// <summary>Creates a baseline from stored fingerprints.</summary>
    /// <param name="fingerprints">Fingerprints of known findings.</param>
    public Baseline(IEnumerable<string> fingerprints)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);
        _fingerprints = new HashSet<string>(fingerprints, StringComparer.Ordinal);
    }

    /// <summary>All fingerprints, ordered for stable serialization.</summary>
    public IReadOnlyList<string> Fingerprints
        => _fingerprints.Order(StringComparer.Ordinal).ToArray();

    /// <summary>Creates a baseline that accepts all given findings as known.</summary>
    /// <param name="findings">The findings to record.</param>
    public static Baseline FromFindings(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        return new Baseline(findings.Select(FindingFingerprint.Of));
    }

    /// <summary>Whether the finding is already known.</summary>
    /// <param name="finding">The finding to check.</param>
    public bool Contains(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return _fingerprints.Contains(FindingFingerprint.Of(finding));
    }
}
