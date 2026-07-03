using ConfigLens.Application.Reporting;
using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Tests.Reporting;

/// <summary>
/// Deterministic report used by renderer snapshot tests: fixed findings,
/// fixed version, no machine-specific paths.
/// </summary>
public static class SampleReports
{
    public static ScanReport Sample() => new(
        "src/DemoApp",
        ["Development", "Production"],
        [
            new Finding(
                RuleIds.MissingKey,
                Severity.Error,
                Confidence.High,
                "Configuration key 'Database:Host' is read in code but missing for environment(s) 'Production'.",
                new SourceLocation("Program.cs", 12),
                "Add 'Database:Host' to the affected configuration files, or remove the read."),
            new Finding(
                RuleIds.EnvironmentDrift,
                Severity.Warning,
                Confidence.High,
                "Configuration key 'Features:Beta' is defined for environment 'Development' but missing for environment 'Production'.",
                new SourceLocation("appsettings.Development.json", 3),
                "Add 'Features:Beta' to appsettings.Production.json, or move it to the base appsettings.json if it applies to all environments."),
            new Finding(
                RuleIds.DeadConfiguration,
                Severity.Info,
                Confidence.Medium,
                "Configuration key 'Legacy:Mode' is never read by the scanned code.",
                new SourceLocation("appsettings.json", 8),
                "Remove 'Legacy:Mode' from the configuration, or wire it up in code."),
        ],
        SuppressedCount: 2,
        new HealthScore(86, 13.6m),
        "1.2.3-test");

    public static ScanReport Empty() => new(
        "src/CleanApp",
        [],
        [],
        SuppressedCount: 0,
        HealthScore.Perfect,
        "1.2.3-test");
}
