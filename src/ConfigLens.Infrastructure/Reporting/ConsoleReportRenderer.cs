using ConfigLens.Application.Reporting;
using ConfigLens.Domain;
using Spectre.Console;

namespace ConfigLens.Infrastructure.Reporting;

/// <summary>
/// Rich console output: findings table, per-rule breakdown and the Config
/// Health Score panel. Writes to an injected <see cref="IAnsiConsole"/> so
/// tests can capture the output.
/// </summary>
public sealed class ConsoleReportRenderer
{
    private readonly IAnsiConsole _console;

    /// <summary>Creates the renderer for the given console.</summary>
    /// <param name="console">Target console; inject a test console in tests.</param>
    public ConsoleReportRenderer(IAnsiConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
    }

    /// <summary>Writes the full report to the console.</summary>
    /// <param name="report">The report to render.</param>
    public void Render(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        _console.MarkupLine($"[bold]ConfigLens[/] [grey]{Markup.Escape(report.ToolVersion)}[/] — scanned [teal]{Markup.Escape(report.RootPath)}[/]");
        if (report.Environments.Count > 0)
        {
            _console.MarkupLine($"[grey]Environments: {Markup.Escape(string.Join(", ", report.Environments))}[/]");
        }

        _console.WriteLine();

        if (report.Findings.Count > 0)
        {
            RenderFindingsTable(report);
            RenderRuleBreakdown(report);
        }
        else
        {
            _console.MarkupLine("[green]No findings.[/]");
        }

        if (report.SuppressedCount > 0)
        {
            _console.MarkupLine($"[grey]{report.SuppressedCount} finding(s) suppressed by baseline.[/]");
        }

        _console.WriteLine();
        RenderScore(report);
    }

    private void RenderFindingsTable(ScanReport report)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Rule");
        table.AddColumn("Severity");
        table.AddColumn("Confidence");
        table.AddColumn("Location");
        table.AddColumn("Message");

        foreach (var finding in report.Findings)
        {
            table.AddRow(
                Markup.Escape(finding.RuleId),
                $"[{ColorOf(finding.Severity)}]{finding.Severity}[/]",
                finding.Confidence.ToString(),
                Markup.Escape(finding.Location.ToString()),
                Markup.Escape(finding.Message));
        }

        _console.Write(table);
    }

    private void RenderRuleBreakdown(ScanReport report)
    {
        var breakdown = new Table().Border(TableBorder.Simple).Title("Findings per rule");
        breakdown.AddColumn("Rule");
        breakdown.AddColumn("Count");

        foreach (var group in report.Findings
            .GroupBy(finding => finding.RuleId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            breakdown.AddRow(Markup.Escape(group.Key), group.Count().ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        _console.Write(breakdown);
    }

    private static string ColorOf(Severity severity) => severity switch
    {
        Severity.Error => "red",
        Severity.Warning => "yellow",
        _ => "grey",
    };

    private void RenderScore(ScanReport report)
    {
        var color = report.Score.Value >= 90 ? "green" : report.Score.Value >= 70 ? "yellow" : "red";
        _console.Write(new Panel($"[bold {color}]Config Health Score: {report.Score.Value}/100[/]").Border(BoxBorder.Rounded));
        _console.WriteLine();
    }
}
