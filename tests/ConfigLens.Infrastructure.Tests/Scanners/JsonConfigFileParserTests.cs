using System.Text;
using System.Text.Json;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Scanners;

public class JsonConfigFileParserTests
{
    private static IReadOnlyList<ConfigEntry> Parse(string json, string environment = "")
        => JsonConfigFileParser.Parse(Encoding.UTF8.GetBytes(json), "appsettings.json", environment);

    [Fact]
    public void Nested_objects_flatten_to_colon_separated_keys()
    {
        var entries = Parse("""
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              }
            }
            """);

        var entry = entries.ShouldHaveSingleItem();
        entry.Key.Path.ShouldBe("Logging:LogLevel:Default");
        entry.Value.ShouldBe("Information");
    }

    [Fact]
    public void Arrays_get_numeric_index_segments()
    {
        var entries = Parse("""
            {
              "Hosts": ["alpha", "beta"],
              "Nested": [{ "Port": 8080 }]
            }
            """);

        entries.Select(e => (e.Key.Path, e.Value)).ShouldBe(
        [
            ("Hosts:0", "alpha"),
            ("Hosts:1", "beta"),
            ("Nested:0:Port", "8080"),
        ]);
    }

    [Fact]
    public void Scalar_types_are_captured_as_raw_text()
    {
        var entries = Parse("""
            {
              "Int": 42,
              "Float": 3.14,
              "True": true,
              "False": false,
              "Null": null
            }
            """);

        entries.Select(e => e.Value).ShouldBe(["42", "3.14", "true", "false", null]);
    }

    [Fact]
    public void Line_numbers_are_one_based_and_point_at_the_value()
    {
        var entries = Parse("""
            {
              "A": 1,
              "B": {
                "C": 2
              }
            }
            """);

        entries.Single(e => e.Key.Path == "A").Location.Line.ShouldBe(2);
        entries.Single(e => e.Key.Path == "B:C").Location.Line.ShouldBe(4);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_tolerated()
    {
        var entries = Parse("""
            {
              // appsettings files may contain comments
              "App": "Demo",
            }
            """);

        entries.ShouldHaveSingleItem().Value.ShouldBe("Demo");
    }

    [Fact]
    public void Utf8_bom_is_skipped()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("""{ "A": 1 }"""))
            .ToArray();

        var entries = JsonConfigFileParser.Parse(bytes, "appsettings.json", "");

        entries.ShouldHaveSingleItem().Key.Path.ShouldBe("A");
    }

    [Fact]
    public void Duplicate_keys_keep_the_last_value()
    {
        var entries = Parse("""
            {
              "App": "First",
              "App": "Second"
            }
            """);

        entries.ShouldHaveSingleItem().Value.ShouldBe("Second");
    }

    [Fact]
    public void Environment_is_stamped_on_every_entry()
    {
        var entries = Parse("""{ "A": 1 }""", "Production");

        entries.ShouldHaveSingleItem().Environment.ShouldBe("Production");
    }

    [Fact]
    public void Empty_object_produces_no_entries()
    {
        Parse("{}").ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_json_throws_json_exception()
    {
        Should.Throw<JsonException>(() => Parse("{ not json"));
    }
}
