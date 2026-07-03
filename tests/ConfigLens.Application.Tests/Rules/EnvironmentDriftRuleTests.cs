using ConfigLens.Application;
using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Rules;

public class EnvironmentDriftRuleTests
{
    private readonly EnvironmentDriftRule _rule = new();

    private static ConfigEntry Entry(string key, string environment, string file = "appsettings.json", int line = 1)
        => new(ConfigKey.Parse(key), "value", environment, new SourceLocation(file, line));

    private static RuleContext Context(IReadOnlyList<ConfigEntry> entries, params string[] requestedEnvironments)
        => new(new ConfigModel(entries), UsageModel.Empty, new ScanRequest(".", requestedEnvironments));

    [Fact]
    public void Key_missing_in_one_environment_is_reported()
    {
        var context = Context(
        [
            Entry("Features:Beta", "Development", "appsettings.Development.json", 3),
            Entry("Cache:Ttl", "Development", "appsettings.Development.json", 5),
            Entry("Cache:Ttl", "Production", "appsettings.Production.json", 2),
        ]);

        var findings = _rule.Evaluate(context).ToList();

        var finding = findings.ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL002");
        finding.Severity.ShouldBe(Severity.Warning);
        finding.Confidence.ShouldBe(Confidence.High);
        finding.Message.ShouldContain("Features:Beta");
        finding.Message.ShouldContain("Production");
        finding.Location.FilePath.ShouldBe("appsettings.Development.json");
    }

    [Fact]
    public void Key_covered_by_base_file_never_drifts()
    {
        var context = Context(
        [
            Entry("Logging:Level", ConfigModel.BaseEnvironment),
            Entry("Logging:Level", "Production", "appsettings.Production.json"),
            Entry("Other", "Development", "appsettings.Development.json"),
            Entry("Other", "Production", "appsettings.Production.json"),
        ]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Requested_environment_without_file_reports_all_environment_specific_keys()
    {
        var context = Context(
            [Entry("Features:Beta", "Development", "appsettings.Development.json")],
            "Development", "Production");

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.Message.ShouldContain("missing for environment 'Production'");
    }

    [Fact]
    public void Environments_outside_the_requested_scope_are_ignored()
    {
        var context = Context(
        [
            Entry("Features:Beta", "Development", "appsettings.Development.json"),
            Entry("Features:Beta", "Staging", "appsettings.Staging.json"),
            Entry("Features:Beta", "Production", "appsettings.Production.json"),
        ], "Development", "Production");

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Key_comparison_is_case_insensitive()
    {
        var context = Context(
        [
            Entry("features:beta", "Development", "appsettings.Development.json"),
            Entry("Features:Beta", "Production", "appsettings.Production.json"),
        ]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Single_discovered_environment_produces_no_findings()
    {
        var context = Context([Entry("Features:Beta", "Development", "appsettings.Development.json")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Drift_is_reported_in_both_directions()
    {
        var context = Context(
        [
            Entry("OnlyDev", "Development", "appsettings.Development.json"),
            Entry("OnlyProd", "Production", "appsettings.Production.json"),
        ]);

        var findings = _rule.Evaluate(context).ToList();

        findings.Count.ShouldBe(2);
        findings.ShouldContain(f => f.Message.Contains("'OnlyDev'") && f.Message.Contains("missing for environment 'Production'"));
        findings.ShouldContain(f => f.Message.Contains("'OnlyProd'") && f.Message.Contains("missing for environment 'Development'"));
    }
}
