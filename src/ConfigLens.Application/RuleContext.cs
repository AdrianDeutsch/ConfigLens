using ConfigLens.Domain;

namespace ConfigLens.Application;

/// <summary>
/// Everything a rule needs to evaluate. Grows with new models per milestone
/// (the usage model from the Roslyn scanner is added in M2).
/// </summary>
/// <param name="Config">Unified configuration model.</param>
/// <param name="Request">The request driving the scan.</param>
public sealed record RuleContext(ConfigModel Config, ScanRequest Request)
{
    /// <summary>
    /// Environments cross-environment rules operate on: the explicitly requested
    /// ones, or all environments discovered in the configuration files.
    /// </summary>
    public IReadOnlyList<string> EnvironmentsInScope
        => Request.Environments.Count > 0 ? Request.Environments : Config.Environments;
}
