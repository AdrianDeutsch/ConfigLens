using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Domain.Tests;

public class ConfigKeyTests
{
    [Fact]
    public void Parse_splits_path_into_segments()
    {
        var key = ConfigKey.Parse("Logging:LogLevel:Default");

        key.Segments.ShouldBe(["Logging", "LogLevel", "Default"]);
        key.LastSegment.ShouldBe("Default");
        key.Path.ShouldBe("Logging:LogLevel:Default");
    }

    [Fact]
    public void Parse_rejects_empty_segments()
    {
        Should.Throw<FormatException>(() => ConfigKey.Parse("Logging::Default"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_rejects_null_or_whitespace(string? path)
    {
        Should.Throw<ArgumentException>(() => ConfigKey.Parse(path!));
    }

    [Fact]
    public void FromSegments_joins_with_separator()
    {
        var key = ConfigKey.FromSegments(["ConnectionStrings", "Default"]);

        key.Path.ShouldBe("ConnectionStrings:Default");
    }

    [Fact]
    public void FromSegments_rejects_empty_input()
    {
        Should.Throw<ArgumentException>(() => ConfigKey.FromSegments([]));
        Should.Throw<ArgumentException>(() => ConfigKey.FromSegments(["a", ""]));
    }

    [Fact]
    public void Equality_ignores_casing_like_the_configuration_system()
    {
        var lower = ConfigKey.Parse("connectionstrings:default");
        var pascal = ConfigKey.Parse("ConnectionStrings:Default");

        lower.ShouldBe(pascal);
        lower.GetHashCode().ShouldBe(pascal.GetHashCode());
    }

    [Fact]
    public void Different_paths_are_not_equal()
    {
        ConfigKey.Parse("A:B").ShouldNotBe(ConfigKey.Parse("A:C"));
    }

    [Fact]
    public void Keys_work_as_dictionary_keys_case_insensitively()
    {
        var dictionary = new Dictionary<ConfigKey, int>
        {
            [ConfigKey.Parse("Feature:Enabled")] = 1,
        };

        dictionary.ContainsKey(ConfigKey.Parse("feature:enabled")).ShouldBeTrue();
    }
}
