using ConfigLens.Domain;

namespace ConfigLens.Application.Ports;

/// <summary>
/// A single analysis rule evaluated against the unified scan models.
/// Rules are pure: they receive models and produce findings, no I/O.
/// </summary>
public interface IRule
{
    /// <summary>Stable rule identifier, e.g. <c>CL002</c>.</summary>
    string RuleId { get; }

    /// <summary>Evaluates the rule and yields all findings.</summary>
    /// <param name="context">The models produced by the scanners.</param>
    IEnumerable<Finding> Evaluate(RuleContext context);
}
