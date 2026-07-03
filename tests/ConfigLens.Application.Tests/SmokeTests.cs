using ConfigLens.Application;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests;

/// <summary>Placeholder keeping the test pipeline green until M1 delivers real behavior.</summary>
public class SmokeTests
{
    [Fact]
    public void Application_assembly_is_loadable()
    {
        typeof(AssemblyMarker).Assembly.GetName().Name.ShouldBe("ConfigLens.Application");
    }
}
