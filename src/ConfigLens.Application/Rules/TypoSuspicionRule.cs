using ConfigLens.Application.Analysis;
using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL007 — Typo suspicion: a key read in code is missing (CL001 territory),
/// but a key with a small edit distance exists in configuration — likely a
/// typo on one of the two sides. Pure casing differences never get here
/// because all key comparisons are case-insensitive.
/// </summary>
public sealed class TypoSuspicionRule : IRule
{
    private const int MaxEditDistance = 2;
    private const int MinKeyLength = 5;

    /// <inheritdoc />
    public string RuleId => RuleIds.TypoSuspicion;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var configKeys = context.Config.Entries
            .Select(entry => entry.Key)
            .Distinct()
            .ToArray();

        foreach (var (usage, _) in MissingKeyAnalysis.FindMissingUsages(context))
        {
            if (usage.Key.Path.Length < MinKeyLength)
            {
                continue;
            }

            var closest = configKeys
                .Select(candidate => (Key: candidate, Distance: Levenshtein.Distance(usage.Key.Path, candidate.Path)))
                .Where(match => match.Distance is > 0 and <= MaxEditDistance)
                .OrderBy(match => match.Distance)
                .ThenBy(match => match.Key.Path, StringComparer.OrdinalIgnoreCase)
                .Select(match => ((ConfigKey Key, int Distance)?)match)
                .FirstOrDefault();
            if (closest is not { } match)
            {
                continue;
            }

            // One edit away is a strong signal; two edits is a weaker hint.
            var confidence = match.Distance == 1 ? Confidence.Medium : Confidence.Low;

            yield return new Finding(
                RuleId,
                Severity.Warning,
                Min(confidence, usage.Confidence),
                $"Key '{usage.Key}' is not found in configuration, but '{match.Key}' is a close match — possible typo.",
                usage.Location,
                $"Rename one of the two so code and configuration agree (did you mean '{match.Key}'?).");
        }
    }

    private static Confidence Min(Confidence left, Confidence right) => left < right ? left : right;
}
