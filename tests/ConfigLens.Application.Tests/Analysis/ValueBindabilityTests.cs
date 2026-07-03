using ConfigLens.Application.Analysis;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Analysis;

public class ValueBindabilityTests
{
    private static BoundProperty Property(string typeName) => new("P", typeName);

    [Theory]
    [InlineData("System.String", "anything", true)]
    [InlineData("System.Int32", "42", true)]
    [InlineData("System.Int32", "not-a-number", false)]
    [InlineData("System.Int32", "3.14", false)]
    [InlineData("System.Boolean", "true", true)]
    [InlineData("System.Boolean", "yes", false)]
    [InlineData("System.Double", "3.14", true)]
    [InlineData("System.Double", "abc", false)]
    [InlineData("System.TimeSpan", "00:05:00", true)]
    [InlineData("System.TimeSpan", "5 minutes", false)]
    [InlineData("System.Guid", "d3b07384-d9a0-4c9e-8b7a-1f2e3d4c5b6a", true)]
    [InlineData("System.Guid", "not-a-guid", false)]
    [InlineData("System.DateTime", "2026-07-03", true)]
    [InlineData("System.DateTime", "tomorrow", false)]
    [InlineData("System.Char", "x", true)]
    [InlineData("System.Char", "xy", false)]
    public void Primitive_types_validate_against_the_value(string typeName, string value, bool expected)
    {
        ValueBindability.CanBind(Property(typeName), value).ShouldBe(expected);
    }

    [Fact]
    public void Unknown_types_return_null_instead_of_guessing()
    {
        ValueBindability.CanBind(Property("MyApp.ComplexType"), "whatever").ShouldBeNull();
    }

    [Theory]
    [InlineData("Verbose", true)]
    [InlineData("verbose", true)]
    [InlineData("1", true)]
    [InlineData("Silent", false)]
    public void Enums_validate_against_member_names_and_numeric_values(string value, bool expected)
    {
        var property = new BoundProperty("Mode", "MyApp.LogMode", ["Quiet", "Verbose"]);

        ValueBindability.CanBind(property, value).ShouldBe(expected);
    }
}
