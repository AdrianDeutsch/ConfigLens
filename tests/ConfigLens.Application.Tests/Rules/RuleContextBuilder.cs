using ConfigLens.Application;
using ConfigLens.Domain;

namespace ConfigLens.Application.Tests.Rules;

/// <summary>Compact construction of rule contexts for cross-referencing rule tests.</summary>
public static class RuleContextBuilder
{
    public static ConfigEntry Entry(string key, string? value = "value", string environment = ConfigModel.BaseEnvironment, int line = 1)
        => new(ConfigKey.Parse(key), value, environment, new SourceLocation(FileFor(environment), line));

    public static KeyUsage Usage(
        string key,
        KeyUsageKind kind = KeyUsageKind.IndexerAccess,
        Confidence confidence = Confidence.High,
        string? boundTypeName = null,
        IReadOnlyList<BoundProperty>? boundProperties = null,
        int line = 10)
        => new(ConfigKey.Parse(key), kind, confidence, new SourceLocation("Program.cs", line), boundTypeName, boundProperties);

    public static RuleContext Context(
        IReadOnlyList<ConfigEntry> entries,
        IReadOnlyList<KeyUsage> usages,
        bool hasUnresolvedAccesses = false,
        params string[] requestedEnvironments)
        => new(
            new ConfigModel(entries),
            new UsageModel(usages, hasUnresolvedAccesses),
            new ScanRequest(".", requestedEnvironments));

    private static string FileFor(string environment)
        => environment.Length == 0 ? "appsettings.json" : $"appsettings.{environment}.json";
}
