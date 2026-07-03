using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;
using static ConfigLens.Application.Tests.Rules.RuleContextBuilder;

namespace ConfigLens.Application.Tests.Rules;

public class UnboundOptionsRuleTests
{
    private readonly UnboundOptionsRule _rule = new();

    [Fact]
    public void Binding_to_a_missing_section_is_an_error_at_the_binding_site()
    {
        var context = Context(
            [Entry("App:Name")],
            [Usage("Audit", KeyUsageKind.OptionsBinding, boundTypeName: "MyApp.AuditOptions")]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL006");
        finding.Severity.ShouldBe(Severity.Error);
        finding.Location.FilePath.ShouldBe("Program.cs");
        finding.Message.ShouldContain("MyApp.AuditOptions");
        finding.Message.ShouldContain("'Audit'");
    }

    [Fact]
    public void Binding_to_an_existing_section_is_fine()
    {
        var context = Context(
            [Entry("Server:Port", "8080")],
            [Usage("Server", KeyUsageKind.OptionsBinding)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void A_section_existing_in_any_environment_counts_as_bound()
    {
        var context = Context(
            [Entry("Server:Port", "8080", environment: "Production")],
            [Usage("Server", KeyUsageKind.OptionsBinding)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Non_binding_usages_are_ignored()
    {
        var context = Context([Entry("App:Name")], [Usage("Missing", KeyUsageKind.GetSection)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Without_any_scanned_configuration_the_rule_is_silent()
    {
        var context = Context([], [Usage("Audit", KeyUsageKind.OptionsBinding)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }
}
