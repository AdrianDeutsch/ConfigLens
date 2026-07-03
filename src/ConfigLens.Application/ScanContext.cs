using ConfigLens.Domain;

namespace ConfigLens.Application;

/// <summary>
/// Mutable collector that scanners write their results into.
/// Scanners run sequentially; the context is not thread-safe.
/// </summary>
public sealed class ScanContext
{
    private readonly List<ConfigEntry> _configEntries = [];
    private readonly List<KeyUsage> _keyUsages = [];
    private readonly List<Finding> _findings = [];

    /// <summary>Creates a context for one scan run.</summary>
    /// <param name="request">The request driving the scan.</param>
    public ScanContext(ScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
    }

    /// <summary>The request driving the scan.</summary>
    public ScanRequest Request { get; }

    /// <summary>Findings reported directly by scanners (e.g. unresolvable key access).</summary>
    public IReadOnlyList<Finding> Findings => _findings;

    /// <summary>Adds a configuration entry discovered by a scanner.</summary>
    /// <param name="entry">The discovered entry.</param>
    public void AddConfigEntry(ConfigEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _configEntries.Add(entry);
    }

    /// <summary>Adds a resolved configuration read discovered by a scanner.</summary>
    /// <param name="usage">The discovered usage.</param>
    public void AddKeyUsage(KeyUsage usage)
    {
        ArgumentNullException.ThrowIfNull(usage);
        _keyUsages.Add(usage);
    }

    /// <summary>Adds a finding reported directly by a scanner.</summary>
    /// <param name="finding">The finding.</param>
    public void AddFinding(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        _findings.Add(finding);
    }

    /// <summary>Builds the immutable config model from the collected entries.</summary>
    public ConfigModel BuildConfigModel() => new(_configEntries);

    /// <summary>Builds the immutable usage model from the collected key usages.</summary>
    public UsageModel BuildUsageModel()
        => new(_keyUsages, _findings.Any(finding => finding.RuleId == RuleIds.UnresolvableKeyAccess));
}
