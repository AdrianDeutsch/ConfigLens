using ConfigLens.Domain;
using ConfigLens.Infrastructure.Reporting;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Reporting;

public class HtmlReportRendererTests
{
    [Fact]
    public void Html_content_is_encoded()
    {
        var report = SampleReports.Empty() with
        {
            Findings =
            [
                new Finding(
                    "CL004",
                    Severity.Error,
                    Confidence.High,
                    "Value '<script>alert(1)</script>' looks wrong & dangerous.",
                    new SourceLocation("appsettings.json", 1)),
            ],
        };

        var html = new HtmlReportRenderer().Render(report);

        html.ShouldNotContain("<script>alert(1)</script>");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("&amp; dangerous");
    }

    [Fact]
    public void Report_is_self_contained()
    {
        var html = new HtmlReportRenderer().Render(SampleReports.Sample());

        html.ShouldNotContain("http://");
        html.ShouldNotContain("<link");
        html.ShouldNotContain("src=");
    }
}
