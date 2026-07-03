using ConfigLens.Domain;

namespace ConfigLens.Application;

/// <summary>
/// The complete result of one scan run.
/// </summary>
/// <param name="Config">Unified configuration model built by the scanners.</param>
/// <param name="Usage">Unified model of configuration reads in code.</param>
/// <param name="Findings">All findings, deterministically ordered.</param>
public sealed record ScanResult(ConfigModel Config, UsageModel Usage, IReadOnlyList<Finding> Findings);
