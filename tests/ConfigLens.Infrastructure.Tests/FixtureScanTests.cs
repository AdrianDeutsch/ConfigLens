using ConfigLens.Application;
using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests;

/// <summary>
/// Regression armor: runs the full M1 pipeline (JSON scanner → rules) against
/// the fixture projects and asserts exact rule IDs, counts and confidences.
/// </summary>
public class FixtureScanTests
{
    private static Task<ScanResult> ScanFixtureAsync(string fixtureName)
    {
        var orchestrator = new ScanOrchestrator(
            [new JsonConfigScanner()],
            [new EnvironmentDriftRule(), new HardcodedSecretRule()]);
        return orchestrator.ScanAsync(
            new ScanRequest(FixturePaths.Resolve(fixtureName)),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CleanApp_produces_zero_findings()
    {
        var result = await ScanFixtureAsync("CleanApp");

        result.Findings.ShouldBeEmpty();
        result.Config.Entries.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DriftApp_reports_exactly_the_two_drifting_keys()
    {
        var result = await ScanFixtureAsync("DriftApp");

        result.Findings.Count.ShouldBe(2);
        result.Findings.ShouldAllBe(f => f.RuleId == RuleIds.EnvironmentDrift);
        result.Findings.ShouldAllBe(f => f.Severity == Severity.Warning && f.Confidence == Confidence.High);

        result.Findings.ShouldContain(f =>
            f.Message.Contains("'Features:EnableBetaSearch'")
            && f.Message.Contains("missing for environment 'Production'")
            && f.Location.FilePath == "appsettings.Development.json");
        result.Findings.ShouldContain(f =>
            f.Message.Contains("'Smtp:Host'")
            && f.Message.Contains("missing for environment 'Development'")
            && f.Location.FilePath == "appsettings.Production.json");
    }

    [Fact]
    public async Task SecretsApp_reports_all_planted_secrets_with_expected_confidence()
    {
        var result = await ScanFixtureAsync("SecretsApp");

        result.Findings.ShouldAllBe(f => f.RuleId == RuleIds.HardcodedSecret && f.Severity == Severity.Error);
        result.Findings.Count.ShouldBe(6);

        var byKey = result.Findings.ToDictionary(
            f => f.Message.Split('\'')[1],
            f => f.Confidence);

        byKey["ConnectionStrings:Default"].ShouldBe(Confidence.High);
        byKey["Auth:GitHubToken"].ShouldBe(Confidence.High);
        byKey["Auth:ApiKey"].ShouldBe(Confidence.High);
        byKey["Aws:AccessKeyId"].ShouldBe(Confidence.High);
        byKey["Auth:ClientSecret"].ShouldBe(Confidence.Medium);
        byKey["Signing:Material"].ShouldBe(Confidence.Low);
    }

    [Fact]
    public async Task Restricting_environments_suppresses_out_of_scope_drift()
    {
        var orchestrator = new ScanOrchestrator([new JsonConfigScanner()], [new EnvironmentDriftRule()]);

        var result = await orchestrator.ScanAsync(
            new ScanRequest(FixturePaths.Resolve("DriftApp"), ["Production"]),
            TestContext.Current.CancellationToken);

        result.Findings.ShouldBeEmpty();
    }
}
