namespace ConfigLens.Domain;

/// <summary>
/// The unified view over all scanned configuration sources, grouped by environment.
/// Mirrors the layering of Microsoft.Extensions.Configuration: the effective
/// configuration of an environment is the base file overridden by the
/// environment-specific file.
/// </summary>
public sealed class ConfigModel
{
    /// <summary>Environment name of the base <c>appsettings.json</c> file.</summary>
    public const string BaseEnvironment = "";

    private readonly Dictionary<ConfigKey, ConfigEntry> _baseEntries;
    private readonly Dictionary<string, Dictionary<ConfigKey, ConfigEntry>> _environmentEntries;

    /// <summary>Creates the model from the flat list of scanned entries.</summary>
    /// <param name="entries">All entries across all environments; later entries win on key collisions.</param>
    public ConfigModel(IReadOnlyList<ConfigEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Entries = entries;
        _baseEntries = [];
        _environmentEntries = new Dictionary<string, Dictionary<ConfigKey, ConfigEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.IsBase)
            {
                _baseEntries[entry.Key] = entry;
                continue;
            }

            if (!_environmentEntries.TryGetValue(entry.Environment, out var byKey))
            {
                byKey = [];
                _environmentEntries[entry.Environment] = byKey;
            }

            byKey[entry.Key] = entry;
        }

        Environments = _environmentEntries.Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>All scanned entries across all environments.</summary>
    public IReadOnlyList<ConfigEntry> Entries { get; }

    /// <summary>Environment names discovered in the sources, excluding the base file.</summary>
    public IReadOnlyList<string> Environments { get; }

    /// <summary>
    /// Returns the effective configuration of an environment: the base entries
    /// overridden by the environment-specific entries. An environment without
    /// its own file yields the base entries only.
    /// </summary>
    /// <param name="environment">Environment name; <see cref="BaseEnvironment"/> for the base file itself.</param>
    public IReadOnlyDictionary<ConfigKey, ConfigEntry> GetEffectiveEntries(string environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var effective = new Dictionary<ConfigKey, ConfigEntry>(_baseEntries);
        if (_environmentEntries.TryGetValue(environment, out var overrides))
        {
            foreach (var (key, entry) in overrides)
            {
                effective[key] = entry;
            }
        }

        return effective;
    }
}
