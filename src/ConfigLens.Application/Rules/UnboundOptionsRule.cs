using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL006 — Unbound options class: an options type is bound to a configuration
/// section that does not exist in any configuration source. Binding succeeds
/// at startup and yields default values — the failure surfaces much later.
/// </summary>
public sealed class UnboundOptionsRule : IRule
{
    /// <inheritdoc />
    public string RuleId => RuleIds.UnboundOptions;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Config.Entries.Count == 0)
        {
            yield break;
        }

        foreach (var usage in context.Usage.Usages)
        {
            if (usage.Kind != KeyUsageKind.OptionsBinding || SectionExists(context.Config, usage.Key))
            {
                continue;
            }

            yield return new Finding(
                RuleId,
                Severity.Error,
                usage.Confidence,
                $"Options type '{usage.BoundTypeName}' is bound to section '{usage.Key}', which does not exist in any configuration source.",
                usage.Location,
                $"Add a '{usage.Key}' section to the configuration, or fix the section name in the binding.");
        }
    }

    private static bool SectionExists(ConfigModel config, ConfigKey section)
    {
        var prefix = section.Path + ConfigKey.Separator;
        return config.Entries.Any(entry =>
            entry.Key.Equals(section)
            || entry.Key.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
