namespace ConfigLens.Infrastructure.Scanners;

/// <summary>
/// Shared directory traversal for all file-based scanners: walks the tree and
/// skips build output, package and VCS folders.
/// </summary>
internal static class SourceDirectoryWalker
{
    private static readonly HashSet<string> ExcludedDirectories = new(
        ["bin", "obj", "node_modules", ".git", ".vs", "TestResults", "artifacts"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Yields all files under <paramref name="directory"/>, skipping excluded folders.</summary>
    public static IEnumerable<string> EnumerateFiles(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }

        foreach (var subdirectory in Directory.EnumerateDirectories(directory))
        {
            if (ExcludedDirectories.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }

            foreach (var file in EnumerateFiles(subdirectory))
            {
                yield return file;
            }
        }
    }
}
