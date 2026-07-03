using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL003 — Dead configuration: a key exists in configuration but no scanned
/// code reads it, neither directly nor through an enclosing section read or
/// options binding. Framework-consumed keys (Logging, AllowedHosts, Kestrel)
/// are excluded. When the scan hit unresolvable accesses (CL900), findings
/// degrade to Low confidence — the key might be read dynamically (ADR-0002).
/// </summary>
public sealed class DeadConfigurationRule : IRule
{
    /// <summary>Key prefixes consumed by the framework rather than user code.</summary>
    private static readonly string[] FrameworkKeyPrefixes = ["Logging", "AllowedHosts", "Kestrel"];

    /// <inheritdoc />
    public string RuleId => RuleIds.DeadConfiguration;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Usage.Usages.Count == 0)
        {
            // Without any observed reads there is no code side to compare
            // against — flagging the whole configuration would be noise.
            yield break;
        }

        var confidence = context.Usage.HasUnresolvedAccesses ? Confidence.Low : Confidence.Medium;

        var sectionPrefixes = context.Usage.Usages
            .Where(usage => usage.Kind is KeyUsageKind.GetSection or KeyUsageKind.OptionsBinding)
            .Select(usage => usage.Key.Path + ConfigKey.Separator)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var reported = new HashSet<ConfigKey>();
        foreach (var entry in context.Config.Entries)
        {
            if (reported.Contains(entry.Key)
                || IsFrameworkKey(entry.Key)
                || context.Usage.IsUsed(entry.Key)
                || IsCoveredBySection(sectionPrefixes, entry.Key))
            {
                continue;
            }

            reported.Add(entry.Key);
            yield return new Finding(
                RuleId,
                Severity.Info,
                confidence,
                $"Configuration key '{entry.Key}' is never read by the scanned code.",
                entry.Location,
                $"Remove '{entry.Key}' from the configuration, or wire it up in code.");
        }
    }

    private static bool IsFrameworkKey(ConfigKey key)
        => FrameworkKeyPrefixes.Any(prefix =>
            key.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (key.Path.Length == prefix.Length || key.Path[prefix.Length] == ConfigKey.Separator));

    private static bool IsCoveredBySection(IReadOnlyList<string> sectionPrefixes, ConfigKey key)
        => sectionPrefixes.Any(prefix => key.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
