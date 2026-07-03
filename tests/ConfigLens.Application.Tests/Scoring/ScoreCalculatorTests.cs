using ConfigLens.Application.Scoring;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Scoring;

public class ScoreCalculatorTests
{
    private static Finding Make(Severity severity, Confidence confidence)
        => new("CL000", severity, confidence, "message", new SourceLocation("file", 1));

    [Fact]
    public void No_findings_is_a_perfect_score()
    {
        var score = ScoreCalculator.Calculate([]);

        score.Value.ShouldBe(100);
        score.TotalPenalty.ShouldBe(0);
    }

    [Theory]
    [InlineData(Severity.Error, Confidence.High, 10.0)]
    [InlineData(Severity.Error, Confidence.Medium, 6.0)]
    [InlineData(Severity.Error, Confidence.Low, 3.0)]
    [InlineData(Severity.Warning, Confidence.High, 3.0)]
    [InlineData(Severity.Warning, Confidence.Medium, 1.8)]
    [InlineData(Severity.Warning, Confidence.Low, 0.9)]
    [InlineData(Severity.Info, Confidence.High, 1.0)]
    [InlineData(Severity.Info, Confidence.Medium, 0.6)]
    [InlineData(Severity.Info, Confidence.Low, 0.3)]
    public void Penalty_is_severity_weight_scaled_by_confidence(Severity severity, Confidence confidence, double expected)
    {
        ScoreCalculator.PenaltyOf(Make(severity, confidence)).ShouldBe(expected, tolerance: 1e-9);
    }

    [Fact]
    public void Penalties_accumulate_and_round_away_from_zero()
    {
        // 10 + 3 + 0.6 = 13.6 penalty -> 86.4 -> 86; add 0.9 -> 85.5 -> 86 (away from zero).
        var findings = new[]
        {
            Make(Severity.Error, Confidence.High),
            Make(Severity.Warning, Confidence.High),
            Make(Severity.Info, Confidence.Medium),
        };

        ScoreCalculator.Calculate(findings).Value.ShouldBe(86);
        ScoreCalculator.Calculate([.. findings, Make(Severity.Warning, Confidence.Low)]).Value.ShouldBe(86);
    }

    [Fact]
    public void Score_is_floored_at_zero()
    {
        var findings = Enumerable.Repeat(Make(Severity.Error, Confidence.High), 25).ToArray();

        var score = ScoreCalculator.Calculate(findings);

        score.Value.ShouldBe(0);
        score.TotalPenalty.ShouldBe(250);
    }
}
