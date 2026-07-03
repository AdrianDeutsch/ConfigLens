using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;
using static ConfigLens.Application.Tests.Rules.RuleContextBuilder;

namespace ConfigLens.Application.Tests.Rules;

public class DeadConfigurationRuleTests
{
    private readonly DeadConfigurationRule _rule = new();

    [Fact]
    public void Unread_key_is_reported_at_its_config_location()
    {
        var context = Context([Entry("Features:Legacy", line: 5)], [Usage("App:Name")]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL003");
        finding.Severity.ShouldBe(Severity.Info);
        finding.Confidence.ShouldBe(Confidence.Medium);
        finding.Location.ShouldBe(new SourceLocation("appsettings.json", 5));
    }

    [Fact]
    public void Directly_read_keys_are_alive()
    {
        var context = Context([Entry("App:Name")], [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Keys_under_a_read_section_are_alive()
    {
        var context = Context(
            [Entry("Smtp:Host"), Entry("Smtp:Port")],
            [Usage("Smtp", KeyUsageKind.GetSection)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Keys_under_a_bound_section_are_alive()
    {
        var context = Context(
            [Entry("Server:Port")],
            [Usage("Server", KeyUsageKind.OptionsBinding)]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Framework_keys_are_never_reported()
    {
        var context = Context(
            [Entry("Logging:LogLevel:Default"), Entry("AllowedHosts"), Entry("Kestrel:Endpoints:Http:Url")],
            [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Unresolved_accesses_degrade_the_confidence_to_low()
    {
        var context = Context([Entry("Features:Legacy")], [Usage("App:Name")], hasUnresolvedAccesses: true);

        _rule.Evaluate(context).ShouldHaveSingleItem().Confidence.ShouldBe(Confidence.Low);
    }

    [Fact]
    public void Without_any_observed_reads_the_rule_is_silent()
    {
        var context = Context([Entry("Features:Legacy")], []);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Each_key_is_reported_once_across_environments()
    {
        var context = Context(
            [Entry("Features:Legacy"), Entry("Features:Legacy", environment: "Production")],
            [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldHaveSingleItem();
    }
}
