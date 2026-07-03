using ConfigLens.Application.Baselines;
using ConfigLens.Infrastructure.Baselines;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Baselines;

public sealed class JsonBaselineStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("configlens-baseline-tests").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string PathFor(string name) => Path.Combine(_directory, name);

    [Fact]
    public async Task Baseline_round_trips_through_the_file()
    {
        var path = PathFor("baseline.json");
        var original = new Baseline(["aaaa", "bbbb", "cccc"]);

        await JsonBaselineStore.SaveAsync(original, path, TestContext.Current.CancellationToken);
        var loaded = await JsonBaselineStore.LoadAsync(path, TestContext.Current.CancellationToken);

        loaded.Fingerprints.ShouldBe(original.Fingerprints);
    }

    [Fact]
    public async Task Unsupported_content_is_rejected()
    {
        var path = PathFor("invalid.json");
        await File.WriteAllTextAsync(path, """{ "version": 99 }""", TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidDataException>(
            () => JsonBaselineStore.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Saved_file_is_versioned_and_diff_friendly()
    {
        var path = PathFor("baseline.json");
        await JsonBaselineStore.SaveAsync(new Baseline(["abcd"]), path, TestContext.Current.CancellationToken);

        var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        content.ShouldContain("\"version\": 1");
        content.ShouldContain("abcd");
    }
}
