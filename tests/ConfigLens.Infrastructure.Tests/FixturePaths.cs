namespace ConfigLens.Infrastructure.Tests;

/// <summary>
/// Resolves paths to the sample projects in <c>tests/fixtures/</c> by walking up
/// from the test assembly to the repository root (marked by the solution file).
/// </summary>
public static class FixturePaths
{
    /// <summary>Returns the absolute path of a fixture project directory.</summary>
    /// <param name="fixtureName">Directory name under <c>tests/fixtures/</c>, e.g. <c>DriftApp</c>.</param>
    public static string Resolve(string fixtureName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ConfigLens.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Repository root with ConfigLens.slnx not found above the test assembly.");
        }

        var fixture = Path.Combine(directory.FullName, "tests", "fixtures", fixtureName);
        if (!Directory.Exists(fixture))
        {
            throw new DirectoryNotFoundException($"Fixture '{fixtureName}' not found at '{fixture}'.");
        }

        return fixture;
    }
}
