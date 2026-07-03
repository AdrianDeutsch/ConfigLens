namespace ConfigLens.Application;

/// <summary>
/// Describes what to scan.
/// </summary>
/// <param name="RootPath">Directory to scan for configuration and code.</param>
/// <param name="Environments">
/// Environments to check for drift. When empty, the environments discovered
/// from the configuration files are used.
/// </param>
public sealed record ScanRequest(string RootPath, IReadOnlyList<string> Environments)
{
    /// <summary>Creates a request that derives the environments from the discovered files.</summary>
    /// <param name="rootPath">Directory to scan.</param>
    public ScanRequest(string rootPath)
        : this(rootPath, [])
    {
    }
}
