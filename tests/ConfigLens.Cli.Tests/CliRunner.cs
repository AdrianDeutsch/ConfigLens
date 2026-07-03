using System.Diagnostics;

namespace ConfigLens.Cli.Tests;

/// <summary>
/// Runs the built CLI as a real process — the end-to-end contract (arguments,
/// exit codes, output) is what users and CI pipelines see.
/// </summary>
public static class CliRunner
{
    /// <summary>Runs <c>configlens</c> with the given arguments.</summary>
    /// <param name="args">Command-line arguments.</param>
    public static (int ExitCode, string Output) Run(params string[] args)
    {
        var cliPath = Path.Combine(AppContext.BaseDirectory, "configlens.dll");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(cliPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the CLI process.");
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"CLI run timed out. Output so far:{Environment.NewLine}{output}");
        }

        return (process.ExitCode, output);
    }
}
