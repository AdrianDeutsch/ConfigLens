using System.Collections.Concurrent;
using System.Diagnostics;

namespace ConfigLens.Infrastructure.Tests;

/// <summary>
/// Runs <c>dotnet restore</c> on a fixture once per test session so that
/// <c>MSBuildWorkspace</c> can load it with resolved references.
/// </summary>
public static class FixtureRestore
{
    private static readonly ConcurrentDictionary<string, Lazy<bool>> Restored = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ensures the fixture directory has been restored.</summary>
    /// <param name="fixtureDirectory">Absolute path of the fixture project directory.</param>
    public static void EnsureRestored(string fixtureDirectory)
        => _ = Restored.GetOrAdd(fixtureDirectory, directory => new Lazy<bool>(() => Restore(directory))).Value;

    private static bool Restore(string directory)
    {
        var startInfo = new ProcessStartInfo("dotnet", "restore")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start 'dotnet restore'.");
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromMinutes(3)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"'dotnet restore' timed out in '{directory}'.");
        }

        return process.ExitCode == 0
            ? true
            : throw new InvalidOperationException($"'dotnet restore' failed in '{directory}':{Environment.NewLine}{output}");
    }
}
