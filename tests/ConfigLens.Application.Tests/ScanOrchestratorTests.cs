using ConfigLens.Application;
using ConfigLens.Application.Ports;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests;

public class ScanOrchestratorTests
{
    private sealed class StubScanner : IScanner
    {
        public Task ScanAsync(ScanContext context, CancellationToken cancellationToken)
        {
            context.AddConfigEntry(new ConfigEntry(
                ConfigKey.Parse("App:Name"),
                "Demo",
                ConfigModel.BaseEnvironment,
                new SourceLocation("appsettings.json", 2)));
            context.AddFinding(new Finding(
                "CL900",
                Severity.Info,
                Confidence.Low,
                "scanner finding",
                new SourceLocation("Program.cs", 10)));
            return Task.CompletedTask;
        }
    }

    private sealed class StubRule : IRule
    {
        public string RuleId => "CL002";

        public IEnumerable<Finding> Evaluate(RuleContext context)
        {
            context.Config.Entries.ShouldNotBeEmpty();
            yield return new Finding(
                RuleId,
                Severity.Warning,
                Confidence.High,
                "rule finding",
                new SourceLocation("appsettings.json", 2));
        }
    }

    [Fact]
    public async Task Runs_scanners_then_rules_and_orders_findings_deterministically()
    {
        var orchestrator = new ScanOrchestrator([new StubScanner()], [new StubRule()]);

        var result = await orchestrator.ScanAsync(new ScanRequest("."), TestContext.Current.CancellationToken);

        result.Config.Entries.ShouldHaveSingleItem();
        result.Findings.Select(f => f.RuleId).ShouldBe(["CL002", "CL900"]);
    }

    [Fact]
    public async Task Works_without_scanners_and_rules()
    {
        var orchestrator = new ScanOrchestrator([], []);

        var result = await orchestrator.ScanAsync(new ScanRequest("."), TestContext.Current.CancellationToken);

        result.Findings.ShouldBeEmpty();
        result.Config.Entries.ShouldBeEmpty();
    }
}
