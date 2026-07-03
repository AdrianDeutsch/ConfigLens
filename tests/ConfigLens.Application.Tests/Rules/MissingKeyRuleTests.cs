using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;
using static ConfigLens.Application.Tests.Rules.RuleContextBuilder;

namespace ConfigLens.Application.Tests.Rules;

public class MissingKeyRuleTests
{
    private readonly MissingKeyRule _rule = new();

    [Fact]
    public void Read_of_a_key_that_exists_produces_no_finding()
    {
        var context = Context([Entry("App:Name")], [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Read_of_a_missing_key_is_an_error_at_the_code_location()
    {
        var context = Context([Entry("App:Name")], [Usage("Database:Host")]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL001");
        finding.Severity.ShouldBe(Severity.Error);
        finding.Confidence.ShouldBe(Confidence.High);
        finding.Location.FilePath.ShouldBe("Program.cs");
        finding.Message.ShouldContain("Database:Host");
    }

    [Fact]
    public void Confidence_of_the_finding_follows_the_usage_resolution()
    {
        var context = Context([Entry("App:Name")], [Usage("Database:Host", confidence: Confidence.Medium)]);

        _rule.Evaluate(context).ShouldHaveSingleItem().Confidence.ShouldBe(Confidence.Medium);
    }

    [Fact]
    public void Key_present_only_in_one_environment_is_reported_for_the_others()
    {
        var context = Context(
            [Entry("Cache:Ttl", environment: "Development")],
            [Usage("Cache:Ttl")],
            requestedEnvironments: ["Development", "Production"]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.Message.ShouldContain("'Production'");
        finding.Message.ShouldNotContain("'Development'");
    }

    [Fact]
    public void Reading_an_existing_section_as_value_is_not_missing()
    {
        var context = Context([Entry("Smtp:Host")], [Usage("Smtp")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Section_reads_and_bindings_are_not_checked()
    {
        var context = Context(
            [Entry("App:Name")],
            [Usage("Missing", KeyUsageKind.GetSection), Usage("AlsoMissing", KeyUsageKind.OptionsBinding)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Without_any_scanned_configuration_the_rule_is_silent()
    {
        var context = Context([], [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }
}
