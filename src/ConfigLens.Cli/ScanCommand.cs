using System.CommandLine;
using ConfigLens.Application.Reporting;
using Spectre.Console;

namespace ConfigLens.Cli;

/// <summary>
/// Defines the <c>scan</c> command surface: arguments, options and their
/// defaults. All names, defaults and exit codes are a stable public contract
/// from v0.1 on (ADR-0004).
/// </summary>
internal static class ScanCommand
{
    /// <summary>Builds the wired <c>scan</c> command.</summary>
    /// <param name="console">Console the scan writes to.</param>
    public static Command Build(IAnsiConsole console)
    {
        Argument<string> pathArgument = new("path")
        {
            Description = "Directory to scan (solution or project root).",
            DefaultValueFactory = _ => ".",
        };

        Option<string?> environmentsOption = new("--environments", "-e")
        {
            Description = "Comma-separated environments to check for drift (default: discovered from file names).",
        };

        Option<string[]> formatOption = new("--format", "-f")
        {
            Description = "Report format(s) to produce.",
            DefaultValueFactory = _ => ["console"],
            AllowMultipleArgumentsPerToken = true,
        };
        formatOption.AcceptOnlyFromAmong("console", "json", "html", "sarif");

        Option<string> outputOption = new("--output", "-o")
        {
            Description = "Directory file reports are written to.",
            DefaultValueFactory = _ => ".",
        };

        Option<string> failOnOption = new("--fail-on")
        {
            Description = "Severity that makes the scan exit with code 1.",
            DefaultValueFactory = _ => "error",
        };
        failOnOption.AcceptOnlyFromAmong("error", "warning", "none");

        Option<string?> baselineOption = new("--baseline")
        {
            Description = "Baseline file; known findings listed in it are suppressed.",
        };

        Option<bool> writeBaselineOption = new("--write-baseline")
        {
            Description = "Write the baseline file from the current findings and exit 0 (default file: .configlens-baseline.json).",
        };

        Command command = new("scan", "Scan a directory for configuration issues.");
        command.Arguments.Add(pathArgument);
        command.Options.Add(environmentsOption);
        command.Options.Add(formatOption);
        command.Options.Add(outputOption);
        command.Options.Add(failOnOption);
        command.Options.Add(baselineOption);
        command.Options.Add(writeBaselineOption);

        command.SetAction((parseResult, cancellationToken) =>
        {
            var settings = new ScanSettings(
                parseResult.GetValue(pathArgument)!,
                SplitEnvironments(parseResult.GetValue(environmentsOption)),
                parseResult.GetValue(formatOption)!,
                parseResult.GetValue(outputOption)!,
                ParseFailOn(parseResult.GetValue(failOnOption)!),
                parseResult.GetValue(baselineOption),
                parseResult.GetValue(writeBaselineOption));
            return ScanCommandHandler.ExecuteAsync(settings, console, cancellationToken);
        });

        return command;
    }

    private static string[] SplitEnvironments(string? environments)
        => environments is null
            ? []
            : environments.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static FailOnThreshold ParseFailOn(string value) => value.ToUpperInvariant() switch
    {
        "NONE" => FailOnThreshold.None,
        "WARNING" => FailOnThreshold.Warning,
        _ => FailOnThreshold.Error,
    };
}
