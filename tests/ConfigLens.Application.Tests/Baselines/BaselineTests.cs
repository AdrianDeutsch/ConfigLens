using ConfigLens.Application.Baselines;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Baselines;

public class BaselineTests
{
    private static Finding Make(string ruleId, string file = "appsettings.json", string message = "message", int line = 1)
        => new(ruleId, Severity.Error, Confidence.High, message, new SourceLocation(file, line));

    [Fact]
    public void Fingerprint_ignores_the_line_number()
    {
        FindingFingerprint.Of(Make("CL001", line: 1)).ShouldBe(FindingFingerprint.Of(Make("CL001", line: 99)));
    }

    [Fact]
    public void Fingerprint_distinguishes_rule_file_and_message()
    {
        var reference = FindingFingerprint.Of(Make("CL001"));

        FindingFingerprint.Of(Make("CL002")).ShouldNotBe(reference);
        FindingFingerprint.Of(Make("CL001", file: "other.json")).ShouldNotBe(reference);
        FindingFingerprint.Of(Make("CL001", message: "other")).ShouldNotBe(reference);
    }

    [Fact]
    public void Baseline_from_findings_suppresses_exactly_those_findings()
    {
        var known = Make("CL001");
        var baseline = Baseline.FromFindings([known]);

        var result = BaselineFilter.Apply([known, Make("CL002")], baseline);

        result.SuppressedFindings.ShouldHaveSingleItem().RuleId.ShouldBe("CL001");
        result.NewFindings.ShouldHaveSingleItem().RuleId.ShouldBe("CL002");
    }

    [Fact]
    public void Suppression_survives_line_shifts()
    {
        var baseline = Baseline.FromFindings([Make("CL001", line: 10)]);

        var result = BaselineFilter.Apply([Make("CL001", line: 42)], baseline);

        result.NewFindings.ShouldBeEmpty();
        result.SuppressedFindings.ShouldHaveSingleItem();
    }

    [Fact]
    public void Empty_baseline_suppresses_nothing()
    {
        var result = BaselineFilter.Apply([Make("CL001")], Baseline.Empty);

        result.NewFindings.ShouldHaveSingleItem();
        result.SuppressedFindings.ShouldBeEmpty();
    }

    [Fact]
    public void Fingerprints_are_ordered_for_stable_serialization()
    {
        var baseline = Baseline.FromFindings([Make("CL003"), Make("CL001"), Make("CL002")]);

        baseline.Fingerprints.ShouldBe(baseline.Fingerprints.Order(StringComparer.Ordinal).ToArray());
    }
}
