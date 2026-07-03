using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;
using static ConfigLens.Application.Tests.Rules.RuleContextBuilder;

namespace ConfigLens.Application.Tests.Rules;

public class TypeMismatchRuleTests
{
    private readonly TypeMismatchRule _rule = new();

    private static KeyUsage Binding(params BoundProperty[] properties)
        => Usage("Server", KeyUsageKind.OptionsBinding, boundTypeName: "MyApp.ServerOptions", boundProperties: properties);

    [Fact]
    public void Value_that_cannot_bind_is_an_error_at_the_config_location()
    {
        var context = Context(
            [Entry("Server:Port", "not-a-number", line: 3)],
            [Binding(new BoundProperty("Port", "System.Int32"))]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL005");
        finding.Severity.ShouldBe(Severity.Error);
        finding.Location.ShouldBe(new SourceLocation("appsettings.json", 3));
        finding.Message.ShouldContain("System.Int32");
    }

    [Fact]
    public void Valid_values_produce_no_findings()
    {
        var context = Context(
            [Entry("Server:Port", "8080"), Entry("Server:Banner", "hello")],
            [Binding(new BoundProperty("Port", "System.Int32"), new BoundProperty("Banner", "System.String"))]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Enum_values_are_checked_against_member_names()
    {
        var context = Context(
            [Entry("Server:Mode", "Silent")],
            [Binding(new BoundProperty("Mode", "MyApp.LogMode", ["Quiet", "Verbose"]))]);

        _rule.Evaluate(context).ShouldHaveSingleItem().Message.ShouldContain("Silent");
    }

    [Fact]
    public void Unknown_property_types_are_skipped()
    {
        var context = Context(
            [Entry("Server:Complex", "whatever")],
            [Binding(new BoundProperty("Complex", "MyApp.ComplexType"))]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Mismatches_are_found_in_every_environment_file()
    {
        var context = Context(
            [Entry("Server:Port", "8080"), Entry("Server:Port", "oops", environment: "Production")],
            [Binding(new BoundProperty("Port", "System.Int32"))]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.Location.FilePath.ShouldBe("appsettings.Production.json");
    }

    [Fact]
    public void Empty_values_are_not_type_checked()
    {
        var context = Context(
            [Entry("Server:Port", "")],
            [Binding(new BoundProperty("Port", "System.Int32"))]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }
}
