namespace ConfigLens.Domain;

/// <summary>
/// A bindable property of an options class, captured at scan time so the
/// type-mismatch rule can validate config values without any Roslyn types
/// leaking out of Infrastructure (ADR-0001).
/// </summary>
/// <param name="Name">Property name; binds to the config key <c>{section}:{Name}</c>.</param>
/// <param name="TypeName">Fully-qualified type name, e.g. <c>System.Int32</c>; nullable value types are unwrapped.</param>
/// <param name="EnumMemberNames">Member names when the (unwrapped) type is an enum; otherwise <see langword="null"/>.</param>
public sealed record BoundProperty(
    string Name,
    string TypeName,
    IReadOnlyList<string>? EnumMemberNames = null);
