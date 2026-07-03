using ConfigLens.Application.Ports;
using ConfigLens.Application.Scoring;
using ConfigLens.Domain;

namespace ConfigLens.Application;

/// <summary>
/// Runs all scanners, builds the unified models and evaluates all rules.
/// This is the single entry point the CLI (and tests) drive a scan through.
/// </summary>
public sealed class ScanOrchestrator
{
    private readonly IReadOnlyList<IScanner> _scanners;
    private readonly IReadOnlyList<IRule> _rules;

    /// <summary>Creates the orchestrator from the registered scanners and rules.</summary>
    /// <param name="scanners">Scanners contributing raw data; executed in order.</param>
    /// <param name="rules">Rules evaluated against the unified models.</param>
    public ScanOrchestrator(IEnumerable<IScanner> scanners, IEnumerable<IRule> rules)
    {
        ArgumentNullException.ThrowIfNull(scanners);
        ArgumentNullException.ThrowIfNull(rules);
        _scanners = scanners.ToArray();
        _rules = rules.ToArray();
    }

    /// <summary>Executes the full scan pipeline.</summary>
    /// <param name="request">What to scan.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    public async Task<ScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = new ScanContext(request);
        foreach (var scanner in _scanners)
        {
            await scanner.ScanAsync(context, cancellationToken).ConfigureAwait(false);
        }

        var config = context.BuildConfigModel();
        var usage = context.BuildUsageModel();
        var ruleContext = new RuleContext(config, usage, request);

        // Deterministic ordering makes console output, snapshots and baselines stable.
        var findings = context.Findings
            .Concat(_rules.SelectMany(rule => rule.Evaluate(ruleContext)))
            .OrderBy(finding => finding.RuleId, StringComparer.Ordinal)
            .ThenBy(finding => finding.Location.FilePath, StringComparer.Ordinal)
            .ThenBy(finding => finding.Location.Line)
            .ThenBy(finding => finding.Message, StringComparer.Ordinal)
            .ToArray();

        return new ScanResult(config, usage, findings, ScoreCalculator.Calculate(findings));
    }
}
