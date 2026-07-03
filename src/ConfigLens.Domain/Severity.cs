namespace ConfigLens.Domain;

/// <summary>
/// Severity of a finding, ordered so that higher values are more severe.
/// The numeric order is relied upon by the <c>--fail-on</c> CLI threshold.
/// </summary>
public enum Severity
{
    /// <summary>Informational finding, never fails a build on its own.</summary>
    Info = 0,

    /// <summary>Potential problem that deserves attention (e.g. environment drift).</summary>
    Warning = 1,

    /// <summary>Defect that is expected to break the application at runtime.</summary>
    Error = 2,
}
