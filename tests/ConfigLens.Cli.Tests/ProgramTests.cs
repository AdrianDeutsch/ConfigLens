using ConfigLens.Cli;
using Shouldly;
using Xunit;

namespace ConfigLens.Cli.Tests;

/// <summary>
/// End-to-end CLI tests run the published tool as a process from M4 on.
/// Until then this exercises the stub entry point in-process.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Main_returns_success_exit_code()
    {
        Program.Main([]).ShouldBe(0);
    }
}
