namespace ConfigLens.Application.Reporting;

/// <summary>
/// The stable exit code contract of the CLI (public contract from v0.1 on).
/// </summary>
public static class ExitCodes
{
    /// <summary>Scan completed and no finding reached the fail threshold.</summary>
    public const int Clean = 0;

    /// <summary>Scan completed with findings at or above the fail threshold.</summary>
    public const int FindingsAtOrAboveThreshold = 1;

    /// <summary>The tool itself failed (invalid arguments, unreadable input, crash).</summary>
    public const int ToolError = 2;
}
