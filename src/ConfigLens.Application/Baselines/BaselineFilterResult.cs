using ConfigLens.Domain;

namespace ConfigLens.Application.Baselines;

/// <summary>
/// Result of applying a baseline to scan findings.
/// </summary>
/// <param name="NewFindings">Findings not covered by the baseline.</param>
/// <param name="SuppressedFindings">Known findings suppressed by the baseline.</param>
public sealed record BaselineFilterResult(
    IReadOnlyList<Finding> NewFindings,
    IReadOnlyList<Finding> SuppressedFindings);
