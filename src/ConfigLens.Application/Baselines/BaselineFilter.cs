using ConfigLens.Domain;

namespace ConfigLens.Application.Baselines;

/// <summary>Splits scan findings into new ones and baseline-suppressed ones.</summary>
public static class BaselineFilter
{
    /// <summary>Applies the baseline to a list of findings, preserving order.</summary>
    /// <param name="findings">All findings of a scan.</param>
    /// <param name="baseline">The baseline to apply.</param>
    public static BaselineFilterResult Apply(IReadOnlyList<Finding> findings, Baseline baseline)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(baseline);

        var newFindings = new List<Finding>();
        var suppressed = new List<Finding>();
        foreach (var finding in findings)
        {
            (baseline.Contains(finding) ? suppressed : newFindings).Add(finding);
        }

        return new BaselineFilterResult(newFindings, suppressed);
    }
}
