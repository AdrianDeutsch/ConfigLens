using ConfigLens.Domain;
using ConfigLens.Infrastructure.Scanners.Usage;
using Shouldly;
using Xunit;

namespace ConfigLens.Infrastructure.Tests.Scanners.Usage;

public class ConfigUsageAnalyzerTests
{
    private static UsageAnalysisResult Analyze(string body, string extraDeclarations = "")
    {
        var source = $$"""
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            {{extraDeclarations}}

            public static class Subject
            {
                public static void Run(IConfiguration config, IServiceCollection services, IConfigurationSection section)
                {
                    {{body}}
                }
            }
            """;
        return ConfigUsageAnalyzer.Analyze(TestCompilation.Compile(source), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Indexer_with_string_literal_is_high_confidence()
    {
        var result = Analyze("""_ = config["App:Name"];""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("App:Name");
        usage.Kind.ShouldBe(KeyUsageKind.IndexerAccess);
        usage.Confidence.ShouldBe(Confidence.High);
        result.Findings.ShouldBeEmpty();
    }

    [Fact]
    public void Indexer_with_const_field_is_medium_confidence()
    {
        var result = Analyze(
            """_ = config[Keys.Timeout];""",
            "public static class Keys { public const string Timeout = \"App:Timeout\"; }");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("App:Timeout");
        usage.Confidence.ShouldBe(Confidence.Medium);
    }

    [Fact]
    public void Indexer_with_nameof_is_medium_confidence()
    {
        var result = Analyze("""_ = config[nameof(Subject)];""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("Subject");
        usage.Confidence.ShouldBe(Confidence.Medium);
    }

    [Fact]
    public void Indexer_with_static_readonly_field_is_medium_confidence()
    {
        var result = Analyze(
            """_ = config[Keys.Endpoint];""",
            "public static class Keys { public static readonly string Endpoint = \"Api:Endpoint\"; }");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("Api:Endpoint");
        usage.Confidence.ShouldBe(Confidence.Medium);
    }

    [Fact]
    public void Indexer_with_inline_constant_concatenation_is_medium_confidence()
    {
        var result = Analyze("""_ = config["App" + ":" + "Name"];""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("App:Name");
        usage.Confidence.ShouldBe(Confidence.Medium);
    }

    [Fact]
    public void Indexer_with_dynamic_interpolation_produces_cl900_and_no_usage()
    {
        var result = Analyze("""
            var feature = System.Console.ReadLine();
            _ = config[$"Features:{feature}"];
            """);

        result.Usages.ShouldBeEmpty();
        var finding = result.Findings.ShouldHaveSingleItem();
        finding.RuleId.ShouldBe(RuleIds.UnresolvableKeyAccess);
        finding.Severity.ShouldBe(Severity.Info);
        finding.Confidence.ShouldBe(Confidence.Low);
    }

    [Fact]
    public void Indexer_with_local_variable_produces_cl900()
    {
        var result = Analyze("""
            var key = "App" + ":" + "Name";
            _ = config[key];
            """);

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldHaveSingleItem().RuleId.ShouldBe(RuleIds.UnresolvableKeyAccess);
    }

    [Fact]
    public void GetValue_with_literal_is_high_confidence()
    {
        var result = Analyze("""_ = config.GetValue<int>("App:PageSize");""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("App:PageSize");
        usage.Kind.ShouldBe(KeyUsageKind.GetValue);
        usage.Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void GetSection_with_literal_is_high_confidence()
    {
        var result = Analyze("""_ = config.GetSection("Smtp");""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("Smtp");
        usage.Kind.ShouldBe(KeyUsageKind.GetSection);
        usage.Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void Indexer_on_section_chain_composes_the_full_path()
    {
        var result = Analyze("""_ = config.GetSection("Smtp")["Host"];""");

        result.Usages.Select(u => (u.Key.Path, u.Kind)).ShouldBe(
        [
            ("Smtp:Host", KeyUsageKind.IndexerAccess),
            ("Smtp", KeyUsageKind.GetSection),
        ], ignoreOrder: true);
    }

    [Fact]
    public void Section_chains_with_colon_paths_compose_correctly()
    {
        var result = Analyze("""_ = config.GetSection("A:B").GetSection("C")["D"];""");

        result.Usages.Select(u => u.Key.Path).ShouldBe(["A:B:C:D", "A:B:C", "A:B"], ignoreOrder: true);
    }

    [Fact]
    public void Indexer_on_section_of_unknown_origin_produces_cl900()
    {
        var result = Analyze("""_ = section["Host"];""");

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldHaveSingleItem().RuleId.ShouldBe(RuleIds.UnresolvableKeyAccess);
    }

    [Fact]
    public void Configure_registration_records_an_options_binding()
    {
        var result = Analyze(
            """services.Configure<MyOptions>(config.GetSection("My"));""",
            "public sealed class MyOptions { public int Value { get; set; } }");

        var binding = result.Usages.Single(u => u.Kind == KeyUsageKind.OptionsBinding);
        binding.Key.Path.ShouldBe("My");
        binding.Confidence.ShouldBe(Confidence.High);
        binding.BoundTypeName.ShouldBe("MyOptions");
    }

    [Fact]
    public void Bind_records_an_options_binding_with_the_instance_type()
    {
        var result = Analyze(
            """
            var options = new MyOptions();
            config.GetSection("My").Bind(options);
            """,
            "public sealed class MyOptions { public int Value { get; set; } }");

        var binding = result.Usages.Single(u => u.Kind == KeyUsageKind.OptionsBinding);
        binding.Key.Path.ShouldBe("My");
        binding.BoundTypeName.ShouldBe("MyOptions");
    }

    [Fact]
    public void Generic_Get_records_an_options_binding()
    {
        var result = Analyze(
            """_ = config.GetSection("My").Get<MyOptions>();""",
            "public sealed class MyOptions { public int Value { get; set; } }");

        var binding = result.Usages.Single(u => u.Kind == KeyUsageKind.OptionsBinding);
        binding.Key.Path.ShouldBe("My");
        binding.BoundTypeName.ShouldBe("MyOptions");
    }

    [Fact]
    public void Dictionary_indexer_is_not_a_configuration_usage()
    {
        var result = Analyze("""
            var dictionary = new System.Collections.Generic.Dictionary<string, string>();
            _ = dictionary["App:Name"];
            """);

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldBeEmpty();
    }

    [Fact]
    public void GetSection_with_dynamic_key_produces_exactly_one_cl900_even_when_chained()
    {
        var result = Analyze("""
            var name = System.Console.ReadLine();
            _ = config.GetSection($"Features:{name}")["Enabled"];
            """);

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldHaveSingleItem().RuleId.ShouldBe(RuleIds.UnresolvableKeyAccess);
    }

    [Fact]
    public void GetConnectionString_maps_to_the_connection_strings_section()
    {
        var result = Analyze("""_ = config.GetConnectionString("Default");""");

        var usage = result.Usages.ShouldHaveSingleItem();
        usage.Key.Path.ShouldBe("ConnectionStrings:Default");
        usage.Kind.ShouldBe(KeyUsageKind.GetValue);
        usage.Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void Options_bindings_capture_the_bindable_properties()
    {
        var result = Analyze(
            """services.Configure<MyOptions>(config.GetSection("My"));""",
            """
            public enum LogMode { Quiet, Verbose }

            public class OptionsBase { public string? Inherited { get; set; } }

            public sealed class MyOptions : OptionsBase
            {
                public int Port { get; set; }
                public int? Retries { get; set; }
                public LogMode Mode { get; set; }
                public string ReadOnly { get; } = "";
                public static string Static { get; set; } = "";
                internal string Hidden { get; set; } = "";
            }
            """);

        var binding = result.Usages.Single(u => u.Kind == KeyUsageKind.OptionsBinding);
        var properties = binding.BoundProperties.ShouldNotBeNull();

        properties.Select(p => (p.Name, p.TypeName)).ShouldBe(
        [
            ("Port", "System.Int32"),
            ("Retries", "System.Int32"),
            ("Mode", "LogMode"),
            ("Inherited", "System.String"),
        ], ignoreOrder: true);
        properties.Single(p => p.Name == "Mode").EnumMemberNames.ShouldBe(["Quiet", "Verbose"]);
    }

    [Fact]
    public void Unrelated_methods_with_matching_names_are_ignored()
    {
        var result = Analyze(
            """_ = Other.GetValue<int>(config, "App:PageSize");""",
            """
            public static class Other
            {
                public static T? GetValue<T>(Microsoft.Extensions.Configuration.IConfiguration c, string key) => default;
            }
            """);

        result.Usages.ShouldBeEmpty();
        result.Findings.ShouldBeEmpty();
    }
}
