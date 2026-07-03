namespace ConfigLens.Domain;

/// <summary>
/// Stable rule identifiers. These are a public contract from v0.1 on:
/// they appear in reports, baselines and SARIF output and must never be reused.
/// </summary>
public static class RuleIds
{
    /// <summary>CL002 — key exists for one environment but is missing for another.</summary>
    public const string EnvironmentDrift = "CL002";

    /// <summary>CL004 — hardcoded secret detected in configuration.</summary>
    public const string HardcodedSecret = "CL004";
}
