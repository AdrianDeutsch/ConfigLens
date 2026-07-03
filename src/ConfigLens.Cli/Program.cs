namespace ConfigLens.Cli;

/// <summary>
/// CLI entry point. Command-line parsing (System.CommandLine) and the DI
/// composition root are added in milestone M4; until then this is a stub
/// that keeps the tool packable and end-to-end testable.
/// </summary>
internal static class Program
{
    /// <summary>Runs the ConfigLens CLI.</summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>Exit code: 0 = clean, 1 = findings at/above threshold, 2 = tool error.</returns>
    public static int Main(string[] args)
    {
        Console.WriteLine("configlens 0.1.0-dev — scanning is not implemented yet (milestone M1+).");
        return 0;
    }
}
