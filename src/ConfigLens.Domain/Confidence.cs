namespace ConfigLens.Domain;

/// <summary>
/// How certain the static analysis is about a finding.
/// Confidence is a first-class concept: dynamic key access, reflection and custom
/// providers cannot always be resolved statically, and a guess must never be
/// presented as a fact (see ADR-0002).
/// </summary>
public enum Confidence
{
    /// <summary>Heuristic match only (dynamic segments, partial paths).</summary>
    Low = 0,

    /// <summary>Resolved through one level of indirection (const/readonly string, <c>nameof</c>).</summary>
    Medium = 1,

    /// <summary>Key usage resolved statically and unambiguously.</summary>
    High = 2,
}
