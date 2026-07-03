namespace ConfigLens.Domain;

/// <summary>
/// Stable rule identifiers. These are a public contract from v0.1 on:
/// they appear in reports, baselines and SARIF output and must never be reused.
/// </summary>
public static class RuleIds
{
    /// <summary>CL001 — key is read in code but missing from configuration.</summary>
    public const string MissingKey = "CL001";

    /// <summary>CL002 — key exists for one environment but is missing for another.</summary>
    public const string EnvironmentDrift = "CL002";

    /// <summary>CL003 — key exists in configuration but is never read by code.</summary>
    public const string DeadConfiguration = "CL003";

    /// <summary>CL004 — hardcoded secret detected in configuration.</summary>
    public const string HardcodedSecret = "CL004";

    /// <summary>CL005 — config value cannot bind to the target options property type.</summary>
    public const string TypeMismatch = "CL005";

    /// <summary>CL006 — options class bound to a section that does not exist.</summary>
    public const string UnboundOptions = "CL006";

    /// <summary>CL007 — config key and code key differ only by a small edit distance.</summary>
    public const string TypoSuspicion = "CL007";

    /// <summary>CL900 — key access that static analysis cannot resolve (informational, ADR-0002).</summary>
    public const string UnresolvableKeyAccess = "CL900";
}
