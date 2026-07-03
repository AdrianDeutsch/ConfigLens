using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;
using static ConfigLens.Application.Tests.Rules.RuleContextBuilder;

namespace ConfigLens.Application.Tests.Rules;

public class TypoSuspicionRuleTests
{
    private readonly TypoSuspicionRule _rule = new();

    [Fact]
    public void One_edit_away_is_a_medium_confidence_warning()
    {
        var context = Context([Entry("Smtp:Host")], [Usage("Smtp:Host2")]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.RuleId.ShouldBe("CL007");
        finding.Severity.ShouldBe(Severity.Warning);
        finding.Confidence.ShouldBe(Confidence.Medium);
        finding.Message.ShouldContain("'Smtp:Host'");
        finding.SuggestedFix.ShouldNotBeNull().ShouldContain("Smtp:Host");
    }

    [Fact]
    public void Two_edits_away_is_a_low_confidence_warning()
    {
        var context = Context([Entry("App:Timeout")], [Usage("App:Timeuot")]);

        _rule.Evaluate(context).ShouldHaveSingleItem().Confidence.ShouldBe(Confidence.Low);
    }

    [Fact]
    public void Existing_keys_never_trigger_typo_suspicion()
    {
        var context = Context([Entry("App:Name")], [Usage("App:Name")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void Distant_keys_are_not_suggested()
    {
        var context = Context([Entry("App:Name")], [Usage("Database:ConnectionTimeout")]);

        _rule.Evaluate(context).ShouldBeEmpty();
    }

    [Fact]
    public void The_closest_candidate_wins()
    {
        var context = Context(
            [Entry("App:Timeout"), Entry("App:Timeouts")],
            [Usage("App:Timeot")]);

        var finding = _rule.Evaluate(context).ShouldHaveSingleItem();
        finding.Message.ShouldContain("'App:Timeout'");
    }
}
