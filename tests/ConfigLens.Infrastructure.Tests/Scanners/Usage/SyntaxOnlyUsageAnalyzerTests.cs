using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners.Usage;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Scanners.Usage;

public class SyntaxOnlyUsageAnalyzerTests
{
    private static UsageAnalysisResult Analyze(string source)
        => SyntaxOnlyUsageAnalyzer.Analyze(
            CSharpSyntaxTree.ParseText(source, path: "Test.cs"),
            TestContext.Current.CancellationToken);

    [Fact]
    public void Indexer_on_config_named_receiver_is_low_confidence()
    {
        var result = Analyze("""
            class C { void M(object configuration) { _ = ((dynamic)configuration)["App:Name"]; } }
            """);

        // The cast keeps the receiver text config-like; syntax-only sees no types.
        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("App:Name");
        usage.Confidence.ShouldBe(Confidence.Low);
    }

    [Fact]
    public void Indexer_on_unrelated_receiver_is_ignored()
    {
        var result = Analyze("""
            class C { void M(System.Collections.Generic.Dictionary<string, int> lookup) { _ = lookup["App:Name"]; } }
            """);

        result.Usages.ShouldBeEmpty();
    }

    [Fact]
    public void GetSection_and_GetValue_with_literals_are_low_confidence()
    {
        var result = Analyze("""
            class C
            {
                void M(dynamic anything)
                {
                    anything.GetSection("Smtp");
                    anything.GetValue<int>("App:PageSize");
                }
            }
            """);

        result.Usages.Select(u => (u.Key.Path, u.Kind, u.Confidence)).ShouldBe(
        [
            ("Smtp", KeyUsageKind.GetSection, Confidence.Low),
            ("App:PageSize", KeyUsageKind.GetValue, Confidence.Low),
        ]);
    }

    [Fact]
    public void Dynamic_interpolated_keys_produce_cl900()
    {
        var result = Analyze("""
            class C { void M(dynamic config, string name) { config.GetSection($"Features:{name}"); } }
            """);

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldHaveSingleItem().RuleId.ShouldBe(RuleIds.UnresolvableKeyAccess);
    }
}
