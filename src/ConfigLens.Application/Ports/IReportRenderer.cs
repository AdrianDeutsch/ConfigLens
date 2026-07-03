using ConfigLens.Application.Reporting;

namespace ConfigLens.Application.Ports;

/// <summary>
/// Port for file-based report renderers (JSON, HTML, SARIF). Implementations
/// live in Infrastructure and are selected by format name; adding a format
/// must not touch existing code (Open/Closed, ADR-0001).
/// </summary>
public interface IReportRenderer
{
    /// <summary>Format name as used by the <c>--format</c> option, e.g. <c>json</c>.</summary>
    string Format { get; }

    /// <summary>File extension including the dot, e.g. <c>.json</c>.</summary>
    string FileExtension { get; }

    /// <summary>Renders the report to its textual representation.</summary>
    /// <param name="report">The report to render.</param>
    string Render(ScanReport report);
}
