namespace ConfigLens.Domain;

/// <summary>
/// A hierarchical configuration key path such as <c>Logging:LogLevel:Default</c>.
/// Comparison is case-insensitive to mirror Microsoft.Extensions.Configuration,
/// where <c>ConnectionStrings</c> and <c>connectionstrings</c> address the same value.
/// </summary>
public sealed class ConfigKey : IEquatable<ConfigKey>
{
    /// <summary>Separator between key path segments.</summary>
    public const char Separator = ':';

    private ConfigKey(string path, IReadOnlyList<string> segments)
    {
        Path = path;
        Segments = segments;
    }

    /// <summary>Full key path, e.g. <c>Logging:LogLevel:Default</c>.</summary>
    public string Path { get; }

    /// <summary>Individual path segments in root-to-leaf order.</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>The leaf segment, e.g. <c>Default</c> for <c>Logging:LogLevel:Default</c>.</summary>
    public string LastSegment => Segments[^1];

    /// <summary>Parses a colon-separated key path.</summary>
    /// <param name="path">The key path; must not contain empty segments.</param>
    /// <exception cref="FormatException">The path contains an empty segment.</exception>
    public static ConfigKey Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = path.Split(Separator);
        if (Array.Exists(segments, string.IsNullOrEmpty))
        {
            throw new FormatException($"Configuration key '{path}' contains an empty segment.");
        }

        return new ConfigKey(path, segments);
    }

    /// <summary>Builds a key from individual segments in root-to-leaf order.</summary>
    /// <param name="segments">The path segments; must be non-empty and contain no empty segment.</param>
    public static ConfigKey FromSegments(IReadOnlyList<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0)
        {
            throw new ArgumentException("At least one segment is required.", nameof(segments));
        }

        var copy = segments.ToArray();
        if (Array.Exists(copy, string.IsNullOrEmpty))
        {
            throw new ArgumentException("Segments must not be empty.", nameof(segments));
        }

        return new ConfigKey(string.Join(Separator, copy), copy);
    }

    /// <inheritdoc />
    public bool Equals(ConfigKey? other)
        => other is not null && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ConfigKey);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Path);

    /// <inheritdoc />
    public override string ToString() => Path;
}
