using System.Diagnostics.CodeAnalysis;
using ConfigLens.Application;
using ConfigLens.Application.Baselines;
using ConfigLens.Application.Ports;
using ConfigLens.Application.Reporting;
using ConfigLens.Application.Scoring;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Baselines;
using ConfigLens.Infrastructure.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ConfigLens.Cli;

/// <summary>
/// Executes the <c>scan</c> command: run the pipeline, apply or write the
/// baseline, render the requested reports and map the outcome to the exit
/// code contract (0 clean, 1 findings at/above threshold, 2 tool error).
/// </summary>
internal static class ScanCommandHandler
{
    private const string DefaultBaselineFileName = ".configlens-baseline.json";

    /// <summary>Runs the scan and returns the process exit code.</summary>
    /// <param name="settings">Parsed command options.</param>
    /// <param name="console">Console to write to.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The CLI boundary maps every failure to the documented exit code 2.")]
    public static async Task<int> ExecuteAsync(ScanSettings settings, IAnsiConsole console, CancellationToken cancellationToken)
    {
        try
        {
            return await RunAsync(settings, console, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            console.MarkupLine($"[red]error:[/] {Markup.Escape(exception.Message)}");
            return ExitCodes.ToolError;
        }
    }

    private static async Task<int> RunAsync(ScanSettings settings, IAnsiConsole console, CancellationToken cancellationToken)
    {
        using var services = CliServices.Build(console);

        var request = new ScanRequest(Path.GetFullPath(settings.Path), settings.Environments);
        var result = await services.GetRequiredService<ScanOrchestrator>()
            .ScanAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var visible = result.Findings;
        var suppressedCount = 0;

        if (settings.WriteBaseline)
        {
            var baselinePath = settings.BaselinePath ?? DefaultBaselineFileName;
            await JsonBaselineStore.SaveAsync(Baseline.FromFindings(result.Findings), baselinePath, cancellationToken)
                .ConfigureAwait(false);
            console.MarkupLine($"[grey]Baseline with {result.Findings.Count} finding(s) written to {Markup.Escape(baselinePath)}.[/]");
        }
        else if (settings.BaselinePath is not null)
        {
            var baseline = await JsonBaselineStore.LoadAsync(settings.BaselinePath, cancellationToken).ConfigureAwait(false);
            var filtered = BaselineFilter.Apply(result.Findings, baseline);
            visible = filtered.NewFindings;
            suppressedCount = filtered.SuppressedFindings.Count;
        }

        var report = new ScanReport(
            settings.Path,
            settings.Environments.Count > 0 ? settings.Environments : result.Config.Environments,
            visible,
            suppressedCount,
            ScoreCalculator.Calculate(visible),
            ToolVersion.Current);

        if (settings.Formats.Contains("console", StringComparer.OrdinalIgnoreCase))
        {
            services.GetRequiredService<ConsoleReportRenderer>().Render(report);
        }

        await WriteFileReportsAsync(settings, services, report, console, cancellationToken).ConfigureAwait(false);

        if (settings.WriteBaseline)
        {
            return ExitCodes.Clean;
        }

        return FailOnThresholdEvaluator.ShouldFail(visible, settings.FailOn)
            ? ExitCodes.FindingsAtOrAboveThreshold
            : ExitCodes.Clean;
    }

    private static async Task WriteFileReportsAsync(
        ScanSettings settings,
        ServiceProvider services,
        ScanReport report,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        var renderers = services.GetServices<IReportRenderer>()
            .ToDictionary(renderer => renderer.Format, StringComparer.OrdinalIgnoreCase);

        var fileFormats = settings.Formats
            .Where(format => !string.Equals(format, "console", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var format in fileFormats)
        {
            var renderer = renderers[format];
            Directory.CreateDirectory(settings.OutputDirectory);
            var filePath = Path.Combine(settings.OutputDirectory, $"configlens-report{renderer.FileExtension}");
            await File.WriteAllTextAsync(filePath, renderer.Render(report), cancellationToken).ConfigureAwait(false);
            console.MarkupLine($"[grey]Report written to {Markup.Escape(filePath)}.[/]");
        }
    }
}
