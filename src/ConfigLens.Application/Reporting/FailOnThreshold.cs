namespace ConfigLens.Application.Reporting;

/// <summary>
/// The <c>--fail-on</c> CI gate: which severity makes the scan exit non-zero.
/// </summary>
public enum FailOnThreshold
{
    /// <summary>Never fail on findings; only tool errors produce a non-zero exit code.</summary>
    None = 0,

    /// <summary>Fail when a warning or error is present.</summary>
    Warning = 1,

    /// <summary>Fail only when an error is present (default).</summary>
    Error = 2,
}
