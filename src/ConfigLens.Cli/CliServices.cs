using ConfigLens.Application;
using ConfigLens.Application.Ports;
using ConfigLens.Application.Rules;
using ConfigLens.Infrastructure.Reporting;
using ConfigLens.Infrastructure.Scanners;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ConfigLens.Cli;

/// <summary>
/// DI composition root: scanners, rules and renderers are registered here and
/// nowhere else — adding one must not touch any other code (ADR-0001).
/// </summary>
internal static class CliServices
{
    /// <summary>Builds the service provider for one CLI invocation.</summary>
    /// <param name="console">Console all components write to.</param>
    public static ServiceProvider Build(IAnsiConsole console)
    {
        var services = new ServiceCollection();

        services.AddSingleton(console);

        services.AddSingleton<IScanner, JsonConfigScanner>();
        services.AddSingleton<IScanner, RoslynUsageScanner>();

        services.AddSingleton<IRule, MissingKeyRule>();
        services.AddSingleton<IRule, EnvironmentDriftRule>();
        services.AddSingleton<IRule, DeadConfigurationRule>();
        services.AddSingleton<IRule, HardcodedSecretRule>();
        services.AddSingleton<IRule, TypeMismatchRule>();
        services.AddSingleton<IRule, UnboundOptionsRule>();
        services.AddSingleton<IRule, TypoSuspicionRule>();

        services.AddSingleton<IReportRenderer, JsonReportRenderer>();
        services.AddSingleton<IReportRenderer, HtmlReportRenderer>();
        services.AddSingleton<IReportRenderer, SarifReportRenderer>();
        services.AddSingleton<ConsoleReportRenderer>();

        services.AddSingleton<ScanOrchestrator>();

        return services.BuildServiceProvider();
    }
}
