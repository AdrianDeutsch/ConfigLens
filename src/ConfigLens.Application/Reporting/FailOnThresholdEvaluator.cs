using ConfigLens.Domain;

namespace ConfigLens.Application.Reporting;

/// <summary>Evaluates the <c>--fail-on</c> contract against visible findings.</summary>
public static class FailOnThresholdEvaluator
{
    /// <summary>Whether the findings reach the threshold and the scan must fail.</summary>
    /// <param name="findings">Visible findings after baseline filtering.</param>
    /// <param name="threshold">The configured threshold.</param>
    public static bool ShouldFail(IEnumerable<Finding> findings, FailOnThreshold threshold)
    {
        ArgumentNullException.ThrowIfNull(findings);
        return threshold switch
        {
            FailOnThreshold.None => false,
            FailOnThreshold.Warning => findings.Any(finding => finding.Severity >= Severity.Warning),
            _ => findings.Any(finding => finding.Severity >= Severity.Error),
        };
    }
}
