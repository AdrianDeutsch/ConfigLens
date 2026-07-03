using System.Text.Json;
using ConfigLens.Infrastructure.Tests;
using Shouldly;
using Xunit;

namespace ConfigLens.Cli.Tests;

/// <summary>
/// End-to-end contract tests: run the CLI as a process against fixtures and
/// assert the exit codes, flags and report files (ADR-0004).
/// </summary>
public sealed class CliEndToEndTests : IDisposable
{
    private readonly string _workDirectory = Directory.CreateTempSubdirectory("configlens-e2e").FullName;

    public void Dispose() => Directory.Delete(_workDirectory, recursive: true);

    private static string Fixture(string name)
    {
        var fixture = FixturePaths.Resolve(name);
        FixtureRestore.EnsureRestored(fixture);
        return fixture;
    }

    [Fact]
    public void Clean_fixture_exits_0_with_a_perfect_score()
    {
        var (exitCode, output) = CliRunner.Run("scan", Fixture("CleanApp"));

        exitCode.ShouldBe(0, output);
        output.ShouldContain("No findings.");
        output.ShouldContain("100/100");
    }

    [Fact]
    public void Errors_fail_the_scan_by_default()
    {
        var (exitCode, output) = CliRunner.Run("scan", Fixture("MissingKeyApp"));

        exitCode.ShouldBe(1, output);
        output.ShouldContain("CL001");
    }

    [Fact]
    public void Fail_on_none_never_fails_on_findings()
    {
        var (exitCode, _) = CliRunner.Run("scan", Fixture("MissingKeyApp"), "--fail-on", "none");

        exitCode.ShouldBe(0);
    }

    [Fact]
    public void Info_findings_do_not_fail_the_default_gate()
    {
        var (exitCode, output) = CliRunner.Run("scan", Fixture("DeadConfigApp"));

        exitCode.ShouldBe(0, output);
        output.ShouldContain("CL003");
    }

    [Fact]
    public void Warnings_fail_the_scan_when_fail_on_warning()
    {
        var (exitCode, output) = CliRunner.Run("scan", Fixture("DriftApp"), "--fail-on", "warning");

        exitCode.ShouldBe(1, output);
        output.ShouldContain("CL002");
    }

    [Fact]
    public void Json_report_is_written_with_the_stable_schema()
    {
        var (exitCode, output) = CliRunner.Run(
            "scan", Fixture("MissingKeyApp"), "--format", "json", "--output", _workDirectory);

        exitCode.ShouldBe(1, output);
        var reportPath = Path.Combine(_workDirectory, "configlens-report.json");
        File.Exists(reportPath).ShouldBeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        document.RootElement.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("findings").GetArrayLength().ShouldBe(4);
        document.RootElement.GetProperty("score").GetProperty("value").GetInt32().ShouldBe(79);
    }

    [Fact]
    public void Baseline_round_trip_suppresses_known_findings()
    {
        var fixture = Fixture("MissingKeyApp");
        var baselinePath = Path.Combine(_workDirectory, "baseline.json");

        var (writeExit, writeOutput) = CliRunner.Run(
            "scan", fixture, "--baseline", baselinePath, "--write-baseline");
        writeExit.ShouldBe(0, writeOutput);
        File.Exists(baselinePath).ShouldBeTrue();

        var (scanExit, scanOutput) = CliRunner.Run("scan", fixture, "--baseline", baselinePath);
        scanExit.ShouldBe(0, scanOutput);
        scanOutput.ShouldContain("suppressed by baseline");
        scanOutput.ShouldContain("100/100");
    }

    [Fact]
    public void Nonexistent_path_is_a_tool_error()
    {
        var (exitCode, output) = CliRunner.Run("scan", Path.Combine(_workDirectory, "does-not-exist"));

        exitCode.ShouldBe(2, output);
        output.ShouldContain("error");
    }
}
