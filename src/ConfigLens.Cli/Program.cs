using System.CommandLine;
using ConfigLens.Application.Reporting;
using Spectre.Console;

namespace ConfigLens.Cli;

/// <summary>
/// CLI entry point: wires the command tree and maps argument errors to the
/// exit code contract (parse errors are tool errors, exit code 2).
/// </summary>
internal static class Program
{
    /// <summary>Runs the ConfigLens CLI.</summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>Exit code: 0 = clean, 1 = findings at/above threshold, 2 = tool error.</returns>
    public static async Task<int> Main(string[] args)
    {
        var console = AnsiConsole.Console;

        var rootCommand = new RootCommand(
            "ConfigLens cross-references your configuration files with the code that actually reads them.");
        rootCommand.Subcommands.Add(ScanCommand.Build(console));

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                console.MarkupLine($"[red]error:[/] {Markup.Escape(error.Message)}");
            }

            return ExitCodes.ToolError;
        }

        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }
}
