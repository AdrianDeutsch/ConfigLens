using ConfigLens.Application.Analysis;
using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL005 — Type mismatch: a configuration value cannot bind to the target
/// property type of an options class. Only statically validatable primitive
/// and enum properties are checked; complex types are skipped (ADR-0002).
/// </summary>
public sealed class TypeMismatchRule : IRule
{
    /// <inheritdoc />
    public string RuleId => RuleIds.TypeMismatch;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var usage in context.Usage.Usages)
        {
            if (usage is not { Kind: KeyUsageKind.OptionsBinding, BoundProperties: { } properties })
            {
                continue;
            }

            foreach (var property in properties)
            {
                var propertyKey = ConfigKey.Parse($"{usage.Key}{ConfigKey.Separator}{property.Name}");
                foreach (var entry in context.Config.Entries)
                {
                    if (!entry.Key.Equals(propertyKey) || string.IsNullOrEmpty(entry.Value))
                    {
                        continue;
                    }

                    if (ValueBindability.CanBind(property, entry.Value) is false)
                    {
                        yield return new Finding(
                            RuleId,
                            Severity.Error,
                            usage.Confidence,
                            $"Value '{entry.Value}' of '{propertyKey}' cannot bind to property '{property.Name}' of type {property.TypeName} on '{usage.BoundTypeName}'.",
                            entry.Location,
                            $"Change the value to a valid {property.TypeName}, or adjust the property type.");
                    }
                }
            }
        }
    }
}
