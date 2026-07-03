namespace ConfigLens.Domain;

/// <summary>
/// A statically resolved read of a configuration key in code.
/// Accesses that cannot be resolved are reported as CL900 findings instead
/// of guessed usages (ADR-0002).
/// </summary>
/// <param name="Key">The key path being read.</param>
/// <param name="Kind">How the key is read.</param>
/// <param name="Confidence">
/// How the key was resolved: literal (<see cref="Confidence.High"/>),
/// one level of indirection like const/<c>nameof</c> (<see cref="Confidence.Medium"/>),
/// or syntax-only heuristics (<see cref="Confidence.Low"/>).
/// </param>
/// <param name="Location">File and line of the access.</param>
/// <param name="BoundTypeName">Fully-qualified options type for <see cref="KeyUsageKind.OptionsBinding"/> usages.</param>
/// <param name="BoundProperties">Bindable properties of the options type for <see cref="KeyUsageKind.OptionsBinding"/> usages.</param>
public sealed record KeyUsage(
    ConfigKey Key,
    KeyUsageKind Kind,
    Confidence Confidence,
    SourceLocation Location,
    string? BoundTypeName = null,
    IReadOnlyList<BoundProperty>? BoundProperties = null);
