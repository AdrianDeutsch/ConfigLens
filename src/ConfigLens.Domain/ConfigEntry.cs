namespace ConfigLens.Domain;

/// <summary>
/// A single leaf value read from a configuration source.
/// </summary>
/// <param name="Key">Normalized hierarchical key.</param>
/// <param name="Value">Raw value as text; <see langword="null"/> for JSON <c>null</c>.</param>
/// <param name="Environment">
/// Environment the entry belongs to, e.g. <c>Production</c> for
/// <c>appsettings.Production.json</c>. <see cref="ConfigModel.BaseEnvironment"/>
/// (empty string) marks the environment-neutral base file.
/// </param>
/// <param name="Location">File and line the value was read from.</param>
public sealed record ConfigEntry(ConfigKey Key, string? Value, string Environment, SourceLocation Location)
{
    /// <summary>Whether the entry comes from the environment-neutral base file.</summary>
    public bool IsBase => Environment.Length == 0;
}
