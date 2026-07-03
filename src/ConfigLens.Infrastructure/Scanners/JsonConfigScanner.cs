using System.Text.RegularExpressions;
using ConfigLens.Application;
using ConfigLens.Application.Ports;

namespace ConfigLens.Infrastructure.Scanners;

/// <summary>
/// Discovers <c>appsettings*.json</c> files under the scan root and turns them
/// into per-environment configuration entries. Build output and package folders
/// are skipped so published copies of the files are not scanned twice.
/// </summary>
public sealed partial class JsonConfigScanner : IScanner
{
    private static readonly HashSet<string> ExcludedDirectories = new(
        ["bin", "obj", "node_modules", ".git", ".vs", "TestResults", "artifacts"],
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task ScanAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var root = Path.GetFullPath(context.Request.RootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Scan root '{root}' does not exist.");
        }

        foreach (var file in Discover(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = FileNameRegex().Match(Path.GetFileName(file));
            if (!match.Success)
            {
                continue;
            }

            var environment = match.Groups["environment"].Value;

            // Paths are stored relative to the root with forward slashes so
            // reports and snapshots are identical across operating systems.
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');

            var content = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
            foreach (var entry in JsonConfigFileParser.Parse(content, relativePath, environment))
            {
                context.AddConfigEntry(entry);
            }
        }
    }

    /// <summary>Walks the directory tree, skipping excluded folders.</summary>
    private static IEnumerable<string> Discover(string directory)
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

            foreach (var file in Discover(subdirectory))
            {
                yield return file;
            }
        }
    }

    [GeneratedRegex(@"^appsettings(?:\.(?<environment>[^.]+))?\.json$", RegexOptions.IgnoreCase)]
    private static partial Regex FileNameRegex();
}
