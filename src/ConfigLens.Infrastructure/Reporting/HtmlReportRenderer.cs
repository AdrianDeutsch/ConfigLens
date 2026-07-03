using System.Globalization;
using System.Net;
using System.Text;
using ConfigLens.Application.Ports;
using ConfigLens.Application.Reporting;
using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Reporting;

/// <summary>
/// Renders a self-contained single-file HTML report: inline CSS, no external
/// assets, so the file can be attached to tickets or CI artifacts as-is.
/// </summary>
public sealed class HtmlReportRenderer : IReportRenderer
{
    /// <inheritdoc />
    public string Format => "html";

    /// <inheritdoc />
    public string FileExtension => ".html";

    /// <inheritdoc />
    public string Render(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <title>ConfigLens Report</title>
            <style>
            body { font-family: -apple-system, 'Segoe UI', Roboto, sans-serif; margin: 2rem auto; max-width: 70rem; color: #1f2328; }
            h1 { font-size: 1.5rem; }
            .meta { color: #59636e; font-size: .9rem; }
            .score { display: inline-block; padding: .4rem 1rem; border-radius: .5rem; color: #fff; font-size: 1.4rem; font-weight: 700; }
            .score.good { background: #1a7f37; }
            .score.medium { background: #9a6700; }
            .score.bad { background: #cf222e; }
            table { border-collapse: collapse; width: 100%; margin-top: 1rem; }
            th, td { border: 1px solid #d1d9e0; padding: .4rem .6rem; text-align: left; font-size: .9rem; vertical-align: top; }
            th { background: #f6f8fa; }
            .sev-error { color: #cf222e; font-weight: 600; }
            .sev-warning { color: #9a6700; font-weight: 600; }
            .sev-info { color: #59636e; }
            code { background: #f6f8fa; padding: .1rem .3rem; border-radius: .3rem; }
            .fix { color: #59636e; }
            </style>
            </head>
            <body>
            <h1>ConfigLens Report</h1>
            <p class="meta">Scanned <code>{{Encode(report.RootPath)}}</code>{{EnvironmentsSuffix(report)}} &middot; ConfigLens {{Encode(report.ToolVersion)}}</p>
            <p><span class="score {{ScoreClass(report.Score.Value)}}">Score {{report.Score.Value}}/100</span></p>
            <p>{{report.CountOf(Severity.Error)}} errors &middot; {{report.CountOf(Severity.Warning)}} warnings &middot; {{report.CountOf(Severity.Info)}} infos{{SuppressedSuffix(report)}}</p>

            """);

        if (report.Findings.Count > 0)
        {
            builder.Append("""
                <table>
                <thead><tr><th>Rule</th><th>Severity</th><th>Confidence</th><th>Location</th><th>Message</th></tr></thead>
                <tbody>

                """);
            foreach (var finding in report.Findings)
            {
                var fix = finding.SuggestedFix is null
                    ? string.Empty
                    : $"<br><span class=\"fix\">{Encode(finding.SuggestedFix)}</span>";
                builder.Append(CultureInfo.InvariantCulture, $"""
                    <tr>
                    <td>{Encode(finding.RuleId)}</td>
                    <td class="sev-{SeverityClass(finding.Severity)}">{finding.Severity}</td>
                    <td>{finding.Confidence}</td>
                    <td><code>{Encode(finding.Location.ToString())}</code></td>
                    <td>{Encode(finding.Message)}{fix}</td>
                    </tr>

                    """);
            }

            builder.Append("</tbody>\n</table>\n");
        }
        else
        {
            builder.Append("<p>No findings. &#127881;</p>\n");
        }

        builder.Append("</body>\n</html>\n");
        return builder.ToString();
    }

    private static string ScoreClass(int score) => score >= 90 ? "good" : score >= 70 ? "medium" : "bad";

    private static string SeverityClass(Severity severity) => severity switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "info",
    };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string EnvironmentsSuffix(ScanReport report)
        => report.Environments.Count == 0
            ? string.Empty
            : $" &middot; environments: {Encode(string.Join(", ", report.Environments))}";

    private static string SuppressedSuffix(ScanReport report)
        => report.SuppressedCount == 0 ? string.Empty : $" &middot; {report.SuppressedCount} suppressed by baseline";
}
