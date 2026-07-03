using ConfigLens.Domain;

namespace ConfigLens.Application.Scoring;

/// <summary>
/// Computes the Config Health Score (ADR-0005): start at 100, subtract a
/// weighted penalty per finding, floor at 0. Severity sets the base penalty
/// (Error 10, Warning 3, Info 1); confidence scales it (High ×1.0, Medium
/// ×0.6, Low ×0.3), so uncertain findings hurt the score less.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>Computes the score for a set of findings.</summary>
    /// <param name="findings">All findings of a scan.</param>
    public static HealthScore Calculate(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        // Decimal keeps the sum exact: with floating point, the order of
        // findings could flip the rounding at .5 midpoints and break the
        // order-independence invariant of ADR-0005.
        var totalPenalty = findings.Sum(PenaltyOf);
        var value = (int)Math.Round(Math.Max(0m, 100m - totalPenalty), MidpointRounding.AwayFromZero);
        return new HealthScore(value, totalPenalty);
    }

    /// <summary>The weighted penalty a single finding subtracts from the score.</summary>
    /// <param name="finding">The finding to weigh.</param>
    public static decimal PenaltyOf(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return BasePenalty(finding.Severity) * ConfidenceFactor(finding.Confidence);
    }

    private static decimal BasePenalty(Severity severity) => severity switch
    {
        Severity.Error => 10m,
        Severity.Warning => 3m,
        Severity.Info => 1m,
        _ => 0m,
    };

    private static decimal ConfidenceFactor(Confidence confidence) => confidence switch
    {
        Confidence.High => 1.0m,
        Confidence.Medium => 0.6m,
        Confidence.Low => 0.3m,
        _ => 0m,
    };
}
