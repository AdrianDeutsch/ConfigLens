using System.Diagnostics.CodeAnalysis;
using ConfigLens.Application;
using ConfigLens.Application.Ports;
using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners.Usage;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace ConfigLens.Infrastructure.Scanners;

/// <summary>
/// Scans the code side: loads every project under the scan root with
/// <see cref="MSBuildWorkspace"/> and analyzes configuration reads with the
/// full semantic model. When a project cannot be loaded or compiled, the
/// scanner degrades to syntax-only analysis with Low confidence instead of
/// failing the scan (ADR-0003).
/// </summary>
public sealed class RoslynUsageScanner : IScanner
{
    /// <inheritdoc />
    public async Task ScanAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        MsBuildBootstrap.EnsureRegistered();

        var root = Path.GetFullPath(context.Request.RootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Scan root '{root}' does not exist.");
        }

        var projectPaths = SourceDirectoryWalker.EnumerateFiles(root)
            .Where(file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.Ordinal);

        foreach (var projectPath in projectPaths)
        {
            var result = await AnalyzeProjectAsync(projectPath, cancellationToken).ConfigureAwait(false);

            foreach (var usage in result.Usages.OrderBy(u => u.Location.FilePath, StringComparer.Ordinal).ThenBy(u => u.Location.Line))
            {
                context.AddKeyUsage(usage with { Location = Relativize(root, usage.Location) });
            }

            foreach (var finding in result.Findings)
            {
                context.AddFinding(finding with { Location = Relativize(root, finding.Location) });
            }
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Graceful degradation to syntax-only analysis is the contract for any project load failure (ADR-0003).")]
    private static async Task<UsageAnalysisResult> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).ConfigureAwait(false);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Without the configuration abstractions in the compilation the
            // semantic analysis cannot classify receivers — fall back.
            if (compilation?.GetTypeByMetadataName("Microsoft.Extensions.Configuration.IConfiguration") is not null)
            {
                var usages = new List<KeyUsage>();
                var findings = new List<Finding>();
                foreach (var tree in compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal))
                {
                    if (IsGeneratedOrBuildOutput(tree.FilePath))
                    {
                        continue;
                    }

                    var result = ConfigUsageAnalyzer.Analyze(compilation.GetSemanticModel(tree), cancellationToken);
                    usages.AddRange(result.Usages);
                    findings.AddRange(result.Findings);
                }

                return new UsageAnalysisResult(usages, findings);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Fall through to the syntax-only path below.
        }

        return await AnalyzeSyntaxOnlyAsync(projectPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Degraded path: parse the project's sources without compiling them.</summary>
    private static async Task<UsageAnalysisResult> AnalyzeSyntaxOnlyAsync(string projectPath, CancellationToken cancellationToken)
    {
        var usages = new List<KeyUsage>();
        var findings = new List<Finding>();
        var projectDirectory = Path.GetDirectoryName(projectPath)!;

        var sourceFiles = SourceDirectoryWalker.EnumerateFiles(projectDirectory)
            .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.Ordinal);

        foreach (var sourceFile in sourceFiles)
        {
            var text = SourceText.From(await File.ReadAllTextAsync(sourceFile, cancellationToken).ConfigureAwait(false));
            var tree = CSharpSyntaxTree.ParseText(text, path: sourceFile, cancellationToken: cancellationToken);
            var result = SyntaxOnlyUsageAnalyzer.Analyze(tree, cancellationToken);
            usages.AddRange(result.Usages);
            findings.AddRange(result.Findings);
        }

        return new UsageAnalysisResult(usages, findings);
    }

    private static bool IsGeneratedOrBuildOutput(string filePath)
        => filePath.Length == 0
            || filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));

    private static SourceLocation Relativize(string root, SourceLocation location)
        => location with { FilePath = Path.GetRelativePath(root, location.FilePath).Replace('\\', '/') };
}
