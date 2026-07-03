using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace ConfigLens.Infrastructure.Tests.Scanners.Usage;

/// <summary>
/// Builds in-memory compilations for analyzer tests: the source under test plus
/// stub declarations of the Microsoft.Extensions abstractions, referenced
/// against the BCL of the test host.
/// </summary>
public static class TestCompilation
{
    /// <summary>Stub declarations matching the fully-qualified names of the real abstractions.</summary>
    public const string ConfigurationStubs = """
        namespace Microsoft.Extensions.Configuration
        {
            public interface IConfiguration
            {
                string? this[string key] { get; set; }
                IConfigurationSection GetSection(string key);
            }

            public interface IConfigurationSection : IConfiguration
            {
                string Key { get; }
            }

            public static class ConfigurationBinder
            {
                public static T? GetValue<T>(this IConfiguration configuration, string key) => default;
                public static T? Get<T>(this IConfiguration configuration) => default;
                public static void Bind(this IConfiguration configuration, object instance) { }
            }
        }

        namespace Microsoft.Extensions.DependencyInjection
        {
            using Microsoft.Extensions.Configuration;

            public interface IServiceCollection { }

            public static class OptionsConfigurationServiceCollectionExtensions
            {
                public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config)
                    where TOptions : class
                    => services;
            }
        }
        """;

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray());

    /// <summary>Compiles the source together with the stubs and returns its semantic model.</summary>
    /// <param name="source">Source of the document under test.</param>
    public static SemanticModel Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
        var compilation = CSharpCompilation.Create(
            "UsageAnalyzerTests",
            [tree, CSharpSyntaxTree.ParseText(ConfigurationStubs, path: "Stubs.cs")],
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("the test source must compile");

        return compilation.GetSemanticModel(tree);
    }
}
