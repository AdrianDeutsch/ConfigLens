using System.Text.Json;
using ConfigLens.Application.Baselines;

namespace ConfigLens.Infrastructure.Baselines;

/// <summary>
/// Persists baselines as a small, diff-friendly JSON file with a version
/// marker so the format can evolve (ADR-0006).
/// </summary>
public static class JsonBaselineStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Loads a baseline from a file.</summary>
    /// <param name="path">Path of the baseline file.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="InvalidDataException">The file is not a valid baseline.</exception>
    public static async Task<Baseline> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var stream = File.OpenRead(path);
        await using var _ = stream.ConfigureAwait(false);
        var document = await JsonSerializer.DeserializeAsync<BaselineDocument>(stream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        if (document is not { Version: 1, Fingerprints: not null })
        {
            throw new InvalidDataException($"'{path}' is not a supported ConfigLens baseline file.");
        }

        return new Baseline(document.Fingerprints);
    }

    /// <summary>Writes a baseline to a file, overwriting an existing one.</summary>
    /// <param name="baseline">The baseline to persist.</param>
    /// <param name="path">Path of the baseline file.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    public static async Task SaveAsync(Baseline baseline, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var document = new BaselineDocument(1, baseline.Fingerprints);
        var stream = File.Create(path);
        await using var _ = stream.ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>On-disk shape of the baseline file.</summary>
    /// <param name="Version">Format version; currently always 1.</param>
    /// <param name="Fingerprints">Fingerprints of known findings.</param>
    private sealed record BaselineDocument(int Version, IReadOnlyList<string> Fingerprints);
}
