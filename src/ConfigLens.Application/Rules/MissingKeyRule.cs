using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL001 — Missing key: a key is read in code but does not exist in the
/// effective configuration of one or more environments in scope. This is the
/// core "works in Development, crashes in Production" defect.
/// </summary>
public sealed class MissingKeyRule : IRule
{
    /// <inheritdoc />
    public string RuleId => RuleIds.MissingKey;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var (usage, missingEnvironments) in MissingKeyAnalysis.FindMissingUsages(context))
        {
            yield return new Finding(
                RuleId,
                Severity.Error,
                usage.Confidence,
                $"Configuration key '{usage.Key}' is read in code but missing {DescribeScope(missingEnvironments)}.",
                usage.Location,
                $"Add '{usage.Key}' to the affected configuration files, or remove the read.");
        }
    }

    private static string DescribeScope(IReadOnlyList<string> environments)
        => environments is [ConfigModel.BaseEnvironment]
            ? "from the configuration"
            : $"for environment(s) {string.Join(", ", environments.Select(environment => $"'{environment}'"))}";
}
