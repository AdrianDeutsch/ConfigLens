using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// Shared computation for CL001 and CL007: which value reads in code have no
/// matching key in the effective configuration of the environments in scope.
/// A key that only exists as a section prefix (has child keys) is not missing —
/// reading it as a value is unusual but the path does exist.
/// </summary>
internal static class MissingKeyAnalysis
{
    /// <summary>Yields value reads whose key is missing, with the affected environments.</summary>
    public static IEnumerable<(KeyUsage Usage, IReadOnlyList<string> MissingEnvironments)> FindMissingUsages(RuleContext context)
    {
        if (context.Config.Entries.Count == 0)
        {
            // No configuration was scanned at all — reporting every read as
            // missing would be noise, not signal.
            yield break;
        }

        IReadOnlyList<string> environments = context.EnvironmentsInScope.Count > 0
            ? context.EnvironmentsInScope
            : [ConfigModel.BaseEnvironment];

        var effectiveByEnvironment = environments.ToDictionary(
            environment => environment,
            context.Config.GetEffectiveEntries,
            StringComparer.OrdinalIgnoreCase);

        foreach (var usage in context.Usage.Usages)
        {
            if (usage.Kind is not (KeyUsageKind.IndexerAccess or KeyUsageKind.GetValue))
            {
                continue;
            }

            var missing = environments
                .Where(environment => !ContainsKeyOrSection(effectiveByEnvironment[environment], usage.Key))
                .ToArray();
            if (missing.Length > 0)
            {
                yield return (usage, missing);
            }
        }
    }

    private static bool ContainsKeyOrSection(IReadOnlyDictionary<ConfigKey, ConfigEntry> effective, ConfigKey key)
    {
        if (effective.ContainsKey(key))
        {
            return true;
        }

        var prefix = key.Path + ConfigKey.Separator;
        return effective.Keys.Any(candidate => candidate.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
