using ConfigLens.Domain;

namespace ConfigLens.Application.Reporting;

/// <summary>
/// Everything a report renderer needs, already baseline-filtered: the visible
/// findings, the score computed from them, and scan metadata.
/// </summary>
/// <param name="RootPath">The scanned directory as given on the command line.</param>
/// <param name="Environments">Environments the scan compared (requested or discovered).</param>
/// <param name="Findings">Visible findings after baseline filtering, deterministically ordered.</param>
/// <param name="SuppressedCount">Number of findings suppressed by the baseline.</param>
/// <param name="Score">Config Health Score of the visible findings.</param>
/// <param name="ToolVersion">ConfigLens version producing the report.</param>
public sealed record ScanReport(
    string RootPath,
    IReadOnlyList<string> Environments,
    IReadOnlyList<Finding> Findings,
    int SuppressedCount,
    HealthScore Score,
    string ToolVersion)
{
    /// <summary>Number of visible findings with the given severity.</summary>
    /// <param name="severity">The severity to count.</param>
    public int CountOf(Severity severity) => Findings.Count(finding => finding.Severity == severity);
}
