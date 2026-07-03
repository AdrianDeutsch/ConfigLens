using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Domain.Tests;

/// <summary>
/// Pins the numeric ordering of <see cref="Severity"/>, which the
/// <c>--fail-on</c> threshold comparison depends on.
/// </summary>
public class SeverityTests
{
    [Fact]
    public void Severity_is_ordered_from_info_to_error()
    {
        ((int)Severity.Info).ShouldBeLessThan((int)Severity.Warning);
        ((int)Severity.Warning).ShouldBeLessThan((int)Severity.Error);
    }

    [Fact]
    public void Confidence_is_ordered_from_low_to_high()
    {
        ((int)Confidence.Low).ShouldBeLessThan((int)Confidence.Medium);
        ((int)Confidence.Medium).ShouldBeLessThan((int)Confidence.High);
    }
}
