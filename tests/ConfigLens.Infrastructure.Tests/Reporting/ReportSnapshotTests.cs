using ConfigLens.Infrastructure.Reporting;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Reporting;

/// <summary>
/// Snapshot tests: any change to the JSON, SARIF or HTML output is an
/// explicit, reviewed diff of the committed <c>*.verified.*</c> files.
/// The JSON schema is a stable contract (ADR-0004).
/// </summary>
public class ReportSnapshotTests
{
    [Fact]
    public Task Json_report()
        => VerifyXunit.Verifier.Verify(new JsonReportRenderer().Render(SampleReports.Sample()), extension: "json");

    [Fact]
    public Task Sarif_report()
        => VerifyXunit.Verifier.Verify(new SarifReportRenderer().Render(SampleReports.Sample()), extension: "json");

    [Fact]
    public Task Html_report()
        => VerifyXunit.Verifier.Verify(new HtmlReportRenderer().Render(SampleReports.Sample()), extension: "html");

    [Fact]
    public Task Json_report_without_findings()
        => VerifyXunit.Verifier.Verify(new JsonReportRenderer().Render(SampleReports.Empty()), extension: "json");
}
