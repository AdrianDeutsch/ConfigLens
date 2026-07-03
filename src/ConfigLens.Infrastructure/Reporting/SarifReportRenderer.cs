using System.Text.Json;
using ConfigLens.Application.Baselines;
using ConfigLens.Application.Ports;
using ConfigLens.Application.Reporting;
using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Reporting;

/// <summary>
/// Renders SARIF 2.1.0 so findings appear in the GitHub Security tab.
/// Severity maps to SARIF levels (Error → error, Warning → warning,
/// Info → note); confidence travels in the result properties.
/// </summary>
public sealed class SarifReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // The report is a data file, not HTML-embedded: keep quotes readable.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc />
    public string Format => "sarif";

    /// <inheritdoc />
    public string FileExtension => ".sarif";

    /// <inheritdoc />
    public string Render(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        // The "$schema" member name cannot be expressed by anonymous types,
        // so the root object is a dictionary with explicit keys.
        var document = new Dictionary<string, object>
        {
            ["$schema"] = "https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new[]
            {
                new
                {
                    Tool = new
                    {
                        Driver = new
                        {
                            Name = "ConfigLens",
                            InformationUri = "https://github.com/AdrianDeutsch/ConfigLens",
                            Version = report.ToolVersion,
                            Rules = report.Findings
                                .Select(finding => finding.RuleId)
                                .Distinct(StringComparer.Ordinal)
                                .Order(StringComparer.Ordinal)
                                .Select(ruleId => new
                                {
                                    Id = ruleId,
                                    HelpUri = $"https://github.com/AdrianDeutsch/ConfigLens/blob/main/docs/rules/{ruleId}.md",
                                })
                                .ToArray(),
                        },
                    },
                    Results = report.Findings.Select(finding => new
                    {
                        RuleId = finding.RuleId,
                        Level = LevelOf(finding.Severity),
                        Message = new { Text = finding.Message },
                        Locations = new[]
                        {
                            new
                            {
                                PhysicalLocation = new
                                {
                                    ArtifactLocation = new { Uri = finding.Location.FilePath },
                                    Region = new { StartLine = Math.Max(1, finding.Location.Line) },
                                },
                            },
                        },
                        PartialFingerprints = new Dictionary<string, string>
                        {
                            ["configLens/v1"] = FindingFingerprint.Of(finding),
                        },
                        Properties = new { Confidence = finding.Confidence.ToString() },
                    }),
                },
            },
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    private static string LevelOf(Severity severity) => severity switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "note",
    };
}
