using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Domain.Tests;

public class ConfigModelTests
{
    private static ConfigEntry Entry(string key, string? value, string environment = ConfigModel.BaseEnvironment)
        => new(ConfigKey.Parse(key), value, environment, new SourceLocation("appsettings.json", 1));

    [Fact]
    public void Environments_lists_discovered_environments_sorted_without_base()
    {
        var model = new ConfigModel(
        [
            Entry("A", "1"),
            Entry("B", "2", "Production"),
            Entry("C", "3", "Development"),
        ]);

        model.Environments.ShouldBe(["Development", "Production"]);
    }

    [Fact]
    public void Effective_entries_overlay_environment_over_base()
    {
        var model = new ConfigModel(
        [
            Entry("Logging:Level", "Information"),
            Entry("App:Name", "Demo"),
            Entry("Logging:Level", "Warning", "Production"),
        ]);

        var effective = model.GetEffectiveEntries("Production");

        effective[ConfigKey.Parse("Logging:Level")].Value.ShouldBe("Warning");
        effective[ConfigKey.Parse("App:Name")].Value.ShouldBe("Demo");
    }

    [Fact]
    public void Effective_entries_for_unknown_environment_fall_back_to_base()
    {
        var model = new ConfigModel([Entry("App:Name", "Demo")]);

        var effective = model.GetEffectiveEntries("Staging");

        effective.Count.ShouldBe(1);
        effective.ContainsKey(ConfigKey.Parse("App:Name")).ShouldBeTrue();
    }

    [Fact]
    public void Later_entries_win_on_duplicate_keys()
    {
        var model = new ConfigModel(
        [
            Entry("App:Name", "First"),
            Entry("App:Name", "Second"),
        ]);

        model.GetEffectiveEntries(ConfigModel.BaseEnvironment)[ConfigKey.Parse("App:Name")]
            .Value.ShouldBe("Second");
    }
}
