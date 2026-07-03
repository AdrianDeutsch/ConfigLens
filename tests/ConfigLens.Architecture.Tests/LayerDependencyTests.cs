using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace ConfigLens.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rules from ADR-0001:
/// Domain references nothing, Application references only Domain,
/// and Roslyn stays behind the Infrastructure boundary.
/// </summary>
public class LayerDependencyTests
{
    private const string ApplicationAssembly = "ConfigLens.Application";
    private const string InfrastructureAssembly = "ConfigLens.Infrastructure";
    private const string CliAssembly = "ConfigLens.Cli";

    [Fact]
    public void Domain_does_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(Domain.Severity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationAssembly, InfrastructureAssembly, CliAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Application_depends_only_on_domain()
    {
        var result = Types.InAssembly(typeof(Application.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureAssembly, CliAssembly)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Domain_and_application_do_not_use_roslyn()
    {
        var result = Types.InAssemblies(
            [
                typeof(Domain.Severity).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
            ])
            .Should()
            .NotHaveDependencyOn("Microsoft.CodeAnalysis")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    private static string FailingTypes(NetArchTest.Rules.TestResult result)
        => result.IsSuccessful
            ? string.Empty
            : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
