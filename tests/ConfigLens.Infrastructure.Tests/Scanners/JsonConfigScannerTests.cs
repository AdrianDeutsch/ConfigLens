using ConfigLens.Application;
using ConfigLens.Infrastructure.Scanners;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Scanners;

public sealed class JsonConfigScannerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("configlens-scanner-tests").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private async Task<ScanContext> ScanAsync()
    {
        var context = new ScanContext(new ScanRequest(_root));
        await new JsonConfigScanner().ScanAsync(context, TestContext.Current.CancellationToken);
        return context;
    }

    [Fact]
    public async Task Discovers_base_and_environment_files()
    {
        WriteFile("appsettings.json", """{ "A": 1 }""");
        WriteFile("appsettings.Production.json", """{ "B": 2 }""");

        var model = (await ScanAsync()).BuildConfigModel();

        model.Environments.ShouldBe(["Production"]);
        model.Entries.Count.ShouldBe(2);
        model.Entries.Single(e => e.IsBase).Key.Path.ShouldBe("A");
    }

    [Fact]
    public async Task Environment_name_comes_from_the_file_name()
    {
        WriteFile("src/Api/appsettings.Staging.json", """{ "A": 1 }""");

        var model = (await ScanAsync()).BuildConfigModel();

        var entry = model.Entries.ShouldHaveSingleItem();
        entry.Environment.ShouldBe("Staging");
        entry.Location.FilePath.ShouldBe("src/Api/appsettings.Staging.json");
    }

    [Fact]
    public async Task Build_output_directories_are_skipped()
    {
        WriteFile("appsettings.json", """{ "A": 1 }""");
        WriteFile("bin/Debug/appsettings.json", """{ "Copied": true }""");
        WriteFile("obj/appsettings.json", """{ "Copied": true }""");

        var model = (await ScanAsync()).BuildConfigModel();

        model.Entries.ShouldHaveSingleItem().Key.Path.ShouldBe("A");
    }

    [Fact]
    public async Task Unrelated_json_files_are_ignored()
    {
        WriteFile("launchSettings.json", """{ "A": 1 }""");
        WriteFile("mysettings.json", """{ "B": 2 }""");

        var model = (await ScanAsync()).BuildConfigModel();

        model.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Missing_root_directory_throws()
    {
        var context = new ScanContext(new ScanRequest(Path.Combine(_root, "does-not-exist")));

        await Should.ThrowAsync<DirectoryNotFoundException>(
            () => new JsonConfigScanner().ScanAsync(context, TestContext.Current.CancellationToken));
    }
}
