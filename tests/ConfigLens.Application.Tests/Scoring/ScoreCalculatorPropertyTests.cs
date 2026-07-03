using ConfigLens.Application.Scoring;
using ConfigLens.Domain;
using FsCheck.Xunit;
using Shouldly;

namespace ConfigLens.Application.Tests.Scoring;

/// <summary>
/// Property-based invariants of the score formula (ADR-0005): the score is
/// always within 0–100, never depends on finding order, and can only drop
/// when findings are added.
/// </summary>
public class ScoreCalculatorPropertyTests
{
    [Property]
    public void Score_is_always_between_0_and_100(Finding[] findings)
    {
        var score = ScoreCalculator.Calculate(findings);

        score.Value.ShouldBeInRange(0, 100);
    }

    [Property]
    public void Score_does_not_depend_on_finding_order(Finding[] findings)
    {
        var forward = ScoreCalculator.Calculate(findings);
        var backward = ScoreCalculator.Calculate(findings.Reverse().ToArray());

        backward.Value.ShouldBe(forward.Value);
    }

    [Property]
    public void Adding_a_finding_never_improves_the_score(Finding[] findings, Finding extra)
    {
        var without = ScoreCalculator.Calculate(findings);
        var with = ScoreCalculator.Calculate([.. findings, extra]);

        with.Value.ShouldBeLessThanOrEqualTo(without.Value);
    }
}
