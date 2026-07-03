using ConfigLens.Application.Ports;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace ConfigLens.Architecture.Tests;

/// <summary>
/// Conventions for analysis rules: every class named *Rule implements
/// <see cref="IRule"/> and is sealed, so the rule engine can rely on the contract.
/// </summary>
public class RuleConventionTests
{
    [Fact]
    public void Rule_classes_implement_IRule_and_are_sealed()
    {
        var result = Types.InAssembly(typeof(IRule).Assembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Rule", StringComparison.Ordinal)
            .Should()
            .ImplementInterface(typeof(IRule))
            .And()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
