using ConfigLens.Application.Reporting;

namespace ConfigLens.Cli;

/// <summary>
/// Parsed options of the <c>scan</c> command.
/// </summary>
/// <param name="Path">Directory to scan, as given on the command line.</param>
/// <param name="Environments">Environments to check for drift; empty = discovered.</param>
/// <param name="Formats">Requested report formats (<c>console</c> and/or file formats).</param>
/// <param name="OutputDirectory">Directory file reports are written to.</param>
/// <param name="FailOn">Severity threshold for the CI gate.</param>
/// <param name="BaselinePath">Baseline file to read (or write with <paramref name="WriteBaseline"/>).</param>
/// <param name="WriteBaseline">Write the baseline instead of failing on findings.</param>
internal sealed record ScanSettings(
    string Path,
    IReadOnlyList<string> Environments,
    IReadOnlyList<string> Formats,
    string OutputDirectory,
    FailOnThreshold FailOn,
    string? BaselinePath,
    bool WriteBaseline);
