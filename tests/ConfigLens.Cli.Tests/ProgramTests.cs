using ConfigLens.Cli;
using Shouldly;
using Xunit;

namespace ConfigLens.Cli.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Help_exits_with_code_0()
    {
        (await Program.Main(["--help"])).ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_option_is_a_tool_error()
    {
        (await Program.Main(["scan", "--bogus"])).ShouldBe(2);
    }

    [Fact]
    public async Task Invalid_format_value_is_a_tool_error()
    {
        (await Program.Main(["scan", ".", "--format", "yaml"])).ShouldBe(2);
    }

    [Fact]
    public async Task Invalid_fail_on_value_is_a_tool_error()
    {
        (await Program.Main(["scan", ".", "--fail-on", "sometimes"])).ShouldBe(2);
    }
}
