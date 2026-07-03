namespace ConfigLens.Domain;

/// <summary>
/// Points at the place a finding or config entry originates from.
/// </summary>
/// <param name="FilePath">File path, relative to the scan root where possible.</param>
/// <param name="Line">1-based line number within the file.</param>
public sealed record SourceLocation(string FilePath, int Line)
{
    /// <inheritdoc />
    public override string ToString() => $"{FilePath}:{Line}";
}
