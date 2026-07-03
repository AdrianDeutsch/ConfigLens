using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL002 — Environment drift: a key introduced by one environment-specific file
/// is missing from another environment's effective configuration.
/// Follows Microsoft.Extensions.Configuration layering: keys in the base
/// <c>appsettings.json</c> are available to every environment and never drift.
/// </summary>
public sealed class EnvironmentDriftRule : IRule
{
    /// <inheritdoc />
    public string RuleId => RuleIds.EnvironmentDrift;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var environments = context.EnvironmentsInScope;
        if (environments.Count < 2 && context.Request.Environments.Count == 0)
        {
            // With at most one discovered environment there is nothing to compare.
            yield break;
        }

        var baseKeys = context.Config.GetEffectiveEntries(ConfigModel.BaseEnvironment);

        // Keys introduced by environment-specific files in scope, with their defining entries.
        var definitions = new Dictionary<ConfigKey, List<ConfigEntry>>();
        foreach (var entry in context.Config.Entries)
        {
            if (entry.IsBase || !environments.Contains(entry.Environment, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!definitions.TryGetValue(entry.Key, out var entries))
            {
                entries = [];
                definitions[entry.Key] = entries;
            }

            entries.Add(entry);
        }

        foreach (var (key, entries) in definitions.OrderBy(pair => pair.Key.Path, StringComparer.OrdinalIgnoreCase))
        {
            if (baseKeys.ContainsKey(key))
            {
                // The base file provides the key to every environment; overrides cannot drift.
                continue;
            }

            var anchor = entries.OrderBy(e => e.Environment, StringComparer.OrdinalIgnoreCase).First();
            foreach (var environment in environments)
            {
                if (entries.Any(e => string.Equals(e.Environment, environment, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                yield return new Finding(
                    RuleId,
                    Severity.Warning,
                    Confidence.High,
                    $"Configuration key '{key}' is defined for environment '{anchor.Environment}' but missing for environment '{environment}'.",
                    anchor.Location,
                    $"Add '{key}' to appsettings.{environment}.json, or move it to the base appsettings.json if it applies to all environments.");
            }
        }
    }
}
