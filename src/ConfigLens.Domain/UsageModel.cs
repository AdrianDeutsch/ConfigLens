namespace ConfigLens.Domain;

/// <summary>
/// The unified view over all configuration reads found in code.
/// </summary>
public sealed class UsageModel
{
    /// <summary>An empty model for scans without a code side.</summary>
    public static readonly UsageModel Empty = new([]);

    private readonly HashSet<ConfigKey> _usedKeys;

    /// <summary>Creates the model from the scanned usages.</summary>
    /// <param name="usages">All key usages across all scanned projects.</param>
    /// <param name="hasUnresolvedAccesses">Whether the scan hit key accesses it could not resolve (CL900).</param>
    public UsageModel(IReadOnlyList<KeyUsage> usages, bool hasUnresolvedAccesses = false)
    {
        ArgumentNullException.ThrowIfNull(usages);
        Usages = usages;
        HasUnresolvedAccesses = hasUnresolvedAccesses;
        _usedKeys = [.. usages.Select(usage => usage.Key)];
    }

    /// <summary>All key usages across all scanned projects.</summary>
    public IReadOnlyList<KeyUsage> Usages { get; }

    /// <summary>
    /// Whether the scan hit key accesses it could not resolve. When true,
    /// absence of a usage is weak evidence — dead-config findings degrade
    /// accordingly (ADR-0002).
    /// </summary>
    public bool HasUnresolvedAccesses { get; }

    /// <summary>Whether any usage reads exactly this key.</summary>
    /// <param name="key">The key to look up (case-insensitive).</param>
    public bool IsUsed(ConfigKey key) => _usedKeys.Contains(key);
}
