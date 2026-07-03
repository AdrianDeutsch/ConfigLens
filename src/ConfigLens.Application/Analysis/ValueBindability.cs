using System.Globalization;
using ConfigLens.Domain;

namespace ConfigLens.Application.Analysis;

/// <summary>
/// Checks whether a raw configuration value can bind to an options property
/// type, mirroring the conversions of Microsoft.Extensions.Configuration's
/// binder for the common primitives. Unknown or complex types return
/// <see langword="null"/> — no false certainty (ADR-0002).
/// </summary>
public static class ValueBindability
{
    /// <summary>
    /// Returns whether <paramref name="value"/> can bind to the property,
    /// or <see langword="null"/> when the type is not validatable statically.
    /// </summary>
    /// <param name="property">The target property.</param>
    /// <param name="value">The raw configuration value.</param>
    public static bool? CanBind(BoundProperty property, string value)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(value);

        if (property.EnumMemberNames is { } members)
        {
            return members.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase)
                || long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        var invariant = CultureInfo.InvariantCulture;
        return property.TypeName switch
        {
            "System.String" or "System.Object" => true,
            "System.Boolean" => bool.TryParse(value, out _),
            "System.Char" => value.Length == 1,
            "System.Byte" => byte.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.SByte" => sbyte.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.Int16" => short.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.UInt16" => ushort.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.Int32" => int.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.UInt32" => uint.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.Int64" => long.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.UInt64" => ulong.TryParse(value, NumberStyles.Integer, invariant, out _),
            "System.Single" => float.TryParse(value, NumberStyles.Float, invariant, out _),
            "System.Double" => double.TryParse(value, NumberStyles.Float, invariant, out _),
            "System.Decimal" => decimal.TryParse(value, NumberStyles.Number, invariant, out _),
            "System.TimeSpan" => TimeSpan.TryParse(value, invariant, out _),
            "System.Guid" => Guid.TryParse(value, out _),
            "System.DateTime" => DateTime.TryParse(value, invariant, DateTimeStyles.None, out _),
            "System.DateTimeOffset" => DateTimeOffset.TryParse(value, invariant, DateTimeStyles.None, out _),
            _ => null,
        };
    }
}
