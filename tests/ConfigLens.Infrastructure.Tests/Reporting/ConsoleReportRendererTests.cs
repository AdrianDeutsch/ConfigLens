using ConfigLens.Infrastructure.Reporting;
using Shouldly;
using Spectre.Console.Testing;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Reporting;

public class ConsoleReportRendererTests
{
    private static string RenderToText(bool empty = false)
    {
        var console = new TestConsole();
        new ConsoleReportRenderer(console).Render(empty ? SampleReports.Empty() : SampleReports.Sample());
        return console.Output;
    }

    [Fact]
    public void Renders_findings_score_and_breakdown()
    {
        var output = RenderToText();

        output.ShouldContain("CL001");
        output.ShouldContain("CL002");
        output.ShouldContain("CL003");
        output.ShouldContain("Program.cs:12");
        output.ShouldContain("Findings per rule");
        output.ShouldContain("Config Health Score: 86/100");
        output.ShouldContain("2 finding(s) suppressed by baseline");
        output.ShouldContain("Development, Production");
    }

    [Fact]
    public void Clean_scan_renders_no_findings_and_a_perfect_score()
    {
        var output = RenderToText(empty: true);

        output.ShouldContain("No findings.");
        output.ShouldContain("Config Health Score: 100/100");
        output.ShouldNotContain("suppressed");
    }
}
