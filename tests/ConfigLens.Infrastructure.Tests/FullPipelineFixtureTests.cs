using ConfigLens.Application;
using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests;

/// <summary>
/// The full M3 pipeline — both scanners, all rules, score — against fixtures
/// engineered to trigger each cross-referencing rule with exact counts.
/// </summary>
public class FullPipelineFixtureTests
{
    private static Task<ScanResult> ScanFixtureAsync(string fixtureName)
    {
        var fixture = FixturePaths.Resolve(fixtureName);
        FixtureRestore.EnsureRestored(fixture);

        var orchestrator = new ScanOrchestrator(
            [new JsonConfigScanner(), new RoslynUsageScanner()],
            [
                new MissingKeyRule(),
                new EnvironmentDriftRule(),
                new DeadConfigurationRule(),
                new HardcodedSecretRule(),
                new TypeMismatchRule(),
                new UnboundOptionsRule(),
                new TypoSuspicionRule(),
            ]);
        return orchestrator.ScanAsync(new ScanRequest(fixture), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CleanApp_has_zero_findings_and_a_perfect_score()
    {
        var result = await ScanFixtureAsync("CleanApp");

        result.Findings.ShouldBeEmpty();
        result.Score.Value.ShouldBe(100);
    }

    [Fact]
    public async Task MissingKeyApp_reports_missing_keys_typo_suspicion_and_the_dead_original()
    {
        var result = await ScanFixtureAsync("MissingKeyApp");

        result.Findings.GroupBy(f => f.RuleId).ToDictionary(g => g.Key, g => g.Count()).ShouldBe(
            new Dictionary<string, int>
            {
                [RuleIds.MissingKey] = 2,
                [RuleIds.DeadConfiguration] = 1,
                [RuleIds.TypoSuspicion] = 1,
            });

        var typo = result.Findings.Single(f => f.RuleId == RuleIds.TypoSuspicion);
        typo.Message.ShouldContain("'App:Timeout'");
        typo.Confidence.ShouldBe(Confidence.Low);

        result.Findings.Where(f => f.RuleId == RuleIds.MissingKey)
            .ShouldAllBe(f => f.Severity == Severity.Error && f.Confidence == Confidence.High);

        // 2×10 (errors) + 3×0.3 (typo) + 1×0.6 (dead) = 21.5 -> 78.5 -> 79.
        result.Score.Value.ShouldBe(79);
    }

    [Fact]
    public async Task DeadConfigApp_reports_only_the_unread_key()
    {
        var result = await ScanFixtureAsync("DeadConfigApp");

        var finding = result.Findings.ShouldHaveSingleItem();
        finding.RuleId.ShouldBe(RuleIds.DeadConfiguration);
        finding.Message.ShouldContain("Features:Legacy");
        finding.Confidence.ShouldBe(Confidence.Medium);
        result.Score.Value.ShouldBe(99);
    }

    [Fact]
    public async Task OptionsApp_reports_type_mismatches_and_the_unbound_section()
    {
        var result = await ScanFixtureAsync("OptionsApp");

        result.Findings.GroupBy(f => f.RuleId).ToDictionary(g => g.Key, g => g.Count()).ShouldBe(
            new Dictionary<string, int>
            {
                [RuleIds.TypeMismatch] = 3,
                [RuleIds.UnboundOptions] = 1,
            });

        result.Findings.ShouldAllBe(f => f.Severity == Severity.Error && f.Confidence == Confidence.High);

        var mismatchedProperties = result.Findings
            .Where(f => f.RuleId == RuleIds.TypeMismatch)
            .Select(f => f.Message.Split('\'')[3])
            .ToArray();
        mismatchedProperties.ShouldBe(["Server:EnableTls", "Server:Mode", "Server:Port"], ignoreOrder: true);

        // 4 High errors = 40 penalty -> score 60.
        result.Score.Value.ShouldBe(60);
    }

    [Fact]
    public async Task DriftApp_still_scores_with_the_full_rule_set()
    {
        var result = await ScanFixtureAsync("DriftApp");

        // DriftApp has no code reading config: the usage side is empty, so
        // CL001/CL003 stay silent and only the two CL002 warnings remain.
        result.Findings.ShouldAllBe(f => f.RuleId == RuleIds.EnvironmentDrift);
        result.Findings.Count.ShouldBe(2);
        result.Score.Value.ShouldBe(94);
    }
}
