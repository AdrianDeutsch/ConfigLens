namespace ConfigLens.Application.Ports;

/// <summary>
/// Port for anything that contributes raw scan data (config entries, key usages,
/// direct findings). Implementations live in Infrastructure and are registered
/// via DI; adding a new source must not touch existing code (Open/Closed, ADR-0001).
/// </summary>
public interface IScanner
{
    /// <summary>Scans the source described by the context and adds its results to it.</summary>
    /// <param name="context">Collector for entries and findings, carries the scan request.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    Task ScanAsync(ScanContext context, CancellationToken cancellationToken);
}
