using System.Text.Json;
using System.Text.Json.Serialization;
using ConfigLens.Application.Baselines;
using ConfigLens.Application.Ports;
using ConfigLens.Application.Reporting;
using ConfigLens.Domain;

namespace ConfigLens.Infrastructure.Reporting;

/// <summary>
/// Renders the versioned JSON report. The schema is a stable public contract
/// from v0.1 on (ADR-0004): fields are only ever added, never renamed or
/// removed, and any breaking change bumps <c>schemaVersion</c>.
/// </summary>
public sealed class JsonReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // The report is a data file, not HTML-embedded: keep quotes readable.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc />
    public string Format => "json";

    /// <inheritdoc />
    public string FileExtension => ".json";

    /// <inheritdoc />
    public string Render(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document = new
        {
            SchemaVersion = 1,
            Tool = new { Name = "ConfigLens", Version = report.ToolVersion },
            Scan = new { Root = report.RootPath, Environments = report.Environments },
            Score = new { report.Score.Value, report.Score.TotalPenalty },
            Summary = new
            {
                Errors = report.CountOf(Severity.Error),
                Warnings = report.CountOf(Severity.Warning),
                Infos = report.CountOf(Severity.Info),
                Suppressed = report.SuppressedCount,
            },
            Findings = report.Findings.Select(finding => new
            {
                finding.RuleId,
                finding.Severity,
                finding.Confidence,
                finding.Message,
                File = finding.Location.FilePath,
                finding.Location.Line,
                finding.SuggestedFix,
                Fingerprint = FindingFingerprint.Of(finding),
            }),
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }
}
