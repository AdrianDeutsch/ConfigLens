using ConfigLens.Application;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests;

/// <summary>
/// End-to-end tests for the Roslyn scanner: fixtures are restored and loaded
/// through MSBuildWorkspace, so these exercise the real semantic path.
/// </summary>
public class RoslynFixtureScanTests
{
    private static async Task<(UsageModel Usage, IReadOnlyList<Finding> Findings)> ScanAsync(string fixtureName)
    {
        var fixture = FixturePaths.Resolve(fixtureName);
        FixtureRestore.EnsureRestored(fixture);

        var context = new ScanContext(new ScanRequest(fixture));
        await new RoslynUsageScanner().ScanAsync(context, TestContext.Current.CancellationToken);
        return (context.BuildUsageModel(), context.Findings);
    }

    [Fact]
    public async Task UsageApp_resolves_every_supported_access_pattern_semantically()
    {
        var (usage, findings) = await ScanAsync("UsageApp");

        findings.ShouldBeEmpty();

        var program = usage.Usages.Where(u => u.Location.FilePath == "Program.cs").ToList();
        program.Select(u => (u.Key.Path, u.Kind, u.Confidence)).ShouldBe(
        [
            ("App:Name", KeyUsageKind.IndexerAccess, Confidence.High),
            ("App:TimeoutSeconds", KeyUsageKind.IndexerAccess, Confidence.Medium),
            ("App:PageSize", KeyUsageKind.GetValue, Confidence.High),
            ("Smtp", KeyUsageKind.GetSection, Confidence.High),
            ("Smtp:Host", KeyUsageKind.IndexerAccess, Confidence.High),
            ("Smtp", KeyUsageKind.GetSection, Confidence.High),
            ("Usage", KeyUsageKind.OptionsBinding, Confidence.High),
            ("Usage", KeyUsageKind.GetSection, Confidence.High),
        ], ignoreOrder: true);

        var binding = program.Single(u => u.Kind == KeyUsageKind.OptionsBinding);
        binding.BoundTypeName.ShouldBe("UsageApp.UsageSettings");
    }

    [Fact]
    public async Task DynamicKeysApp_produces_cl900_notes_and_no_guessed_keys()
    {
        var (usage, findings) = await ScanAsync("DynamicKeysApp");

        var resolved = usage.Usages.ShouldHaveSingleItem();
        resolved.Key.Path.ShouldBe("App:Version");
        resolved.Confidence.ShouldBe(Confidence.High);

        findings.Count.ShouldBe(2);
        findings.ShouldAllBe(f => f.RuleId == RuleIds.UnresolvableKeyAccess);
        findings.ShouldAllBe(f => f.Severity == Severity.Info && f.Confidence == Confidence.Low);
        findings.ShouldAllBe(f => f.Location.FilePath == "Program.cs");
    }

    [Fact]
    public async Task Semantic_analysis_is_used_not_the_degraded_path()
    {
        var (usage, _) = await ScanAsync("UsageApp");

        // The syntax-only fallback marks everything Low; High confidences prove
        // the fixture was loaded with a working semantic model.
        usage.Usages.ShouldContain(u => u.Confidence == Confidence.High);
    }
}
