using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Scanners.Usage;

/// <summary>
/// Result of analyzing one document: resolved key usages plus CL900 findings
/// for accesses that could not be resolved statically.
/// </summary>
/// <param name="Usages">Statically resolved configuration reads.</param>
/// <param name="Findings">CL900 notes for unresolvable accesses.</param>
public sealed record UsageAnalysisResult(IReadOnlyList<KeyUsage> Usages, IReadOnlyList<Finding> Findings);
