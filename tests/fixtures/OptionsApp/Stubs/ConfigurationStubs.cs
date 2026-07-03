// Minimal stand-ins for the Microsoft.Extensions abstractions so the fixture
// compiles without NuGet packages. The fully-qualified names match the real
// ones, which is what the semantic analysis keys on.
#pragma warning disable CA1050, CA1716, CA1815, IDE0060

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
}

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.Extensions.Configuration;

    public interface IServiceCollection
    {
    }

    public static class OptionsConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config)
            where TOptions : class
            => services;
    }
}
