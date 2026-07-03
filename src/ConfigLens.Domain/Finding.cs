namespace ConfigLens.Domain;

/// <summary>
/// A single result produced by a rule or scanner.
/// </summary>
/// <param name="RuleId">Stable rule identifier, e.g. <c>CL002</c>.</param>
/// <param name="Severity">How severe the finding is.</param>
/// <param name="Confidence">How certain the analysis is (see ADR-0002).</param>
/// <param name="Message">Human-readable explanation of the finding.</param>
/// <param name="Location">Where the finding was detected.</param>
/// <param name="SuggestedFix">Optional human-readable suggestion how to fix the finding.</param>
public sealed record Finding(
    string RuleId,
    Severity Severity,
    Confidence Confidence,
    string Message,
    SourceLocation Location,
    string? SuggestedFix = null);
