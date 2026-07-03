using System.Text.RegularExpressions;
using ConfigLens.Application.Analysis;
using ConfigLens.Application.Ports;
using ConfigLens.Domain;

namespace ConfigLens.Application.Rules;

/// <summary>
/// CL004 — Hardcoded secret: connection strings with inline passwords, well-known
/// token formats, secret-suggesting key names and high-entropy values.
/// Detection strength maps to confidence (ADR-0002): exact token formats are
/// <see cref="Confidence.High"/>, an entropy-only match is <see cref="Confidence.Low"/>.
/// </summary>
public sealed partial class HardcodedSecretRule : IRule
{
    private const int MinRandomTokenLength = 24;
    private const double RandomTokenEntropyThreshold = 4.0;
    private const int MinNameBasedValueLength = 8;

    private static readonly HashSet<string> PlaceholderValues = new(
        ["changeme", "change-me", "changeit", "placeholder", "todo", "tbd", "dummy", "sample", "example", "secret", "password", "xxx", "none", "empty"],
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string RuleId => RuleIds.HardcodedSecret;

    /// <inheritdoc />
    public IEnumerable<Finding> Evaluate(RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entry in context.Config.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Value) || IsPlaceholder(entry.Value))
            {
                continue;
            }

            var detection = Detect(entry.Key, entry.Value);
            if (detection is null)
            {
                continue;
            }

            var (confidence, reason) = detection.Value;

            yield return new Finding(
                RuleId,
                Severity.Error,
                confidence,
                $"Possible hardcoded secret in '{entry.Key}' (value '{Redact(entry.Value)}'): {reason}.",
                entry.Location,
                "Move the value to user-secrets, environment variables, or a dedicated secret store and reference it through a configuration provider.");
        }
    }

    /// <summary>
    /// Classifies a value, strongest signal first. Returns <see langword="null"/>
    /// when the value does not look like a secret.
    /// </summary>
    private static (Confidence Confidence, string Reason)? Detect(ConfigKey key, string value)
    {
        if (AwsAccessKeyRegex().IsMatch(value))
        {
            return (Confidence.High, "value matches the AWS access key ID format");
        }

        if (GitHubTokenRegex().IsMatch(value))
        {
            return (Confidence.High, "value matches the GitHub token format");
        }

        if (JwtRegex().IsMatch(value))
        {
            return (Confidence.High, "value looks like a JSON Web Token");
        }

        if (PrivateKeyRegex().IsMatch(value))
        {
            return (Confidence.High, "value contains a private key block");
        }

        var passwordMatch = ConnectionStringPasswordRegex().Match(value);
        if (passwordMatch.Success && !IsPlaceholder(passwordMatch.Groups["value"].Value))
        {
            return (Confidence.High, "connection string contains an inline password");
        }

        var nameSuggestsSecret = SecretKeyNameRegex().IsMatch(key.LastSegment) && LooksLikeCredentialValue(value);
        var looksRandom = LooksLikeRandomToken(value);

        if (nameSuggestsSecret && looksRandom)
        {
            return (Confidence.High, $"key name '{key.LastSegment}' suggests a secret and the value has high entropy");
        }

        if (nameSuggestsSecret)
        {
            return (Confidence.Medium, $"key name '{key.LastSegment}' suggests a secret");
        }

        if (looksRandom)
        {
            return (Confidence.Low, "value is a long high-entropy token (heuristic match)");
        }

        return null;
    }

    /// <summary>
    /// Whether a value is plausible as a credential at all: name-based detection
    /// ignores booleans, numbers and very short values to avoid flagging flags
    /// like <c>RequireHttpsMetadata</c> or <c>TokenLifetimeMinutes</c>.
    /// </summary>
    private static bool LooksLikeCredentialValue(string value)
        => value.Length >= MinNameBasedValueLength
            && !bool.TryParse(value, out _)
            && !double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out _);

    /// <summary>
    /// Whether a value looks like generated key material: long, one token,
    /// token-ish charset and high Shannon entropy. GUIDs are excluded — they are
    /// common as identifiers and rarely secrets on their own.
    /// </summary>
    private static bool LooksLikeRandomToken(string value)
        => value.Length >= MinRandomTokenLength
            && TokenCharsetRegex().IsMatch(value)
            && !Guid.TryParse(value, out _)
            && ShannonEntropy.OfString(value) >= RandomTokenEntropyThreshold;

    private static bool IsPlaceholder(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0
            || PlaceholderValues.Contains(trimmed)
            || PlaceholderPatternRegex().IsMatch(trimmed);
    }

    /// <summary>Shows only a short prefix so reports never leak the secret itself.</summary>
    private static string Redact(string value)
        => value.Length <= 4 ? "***" : $"{value[..4]}…";

    [GeneratedRegex(@"\bAKIA[0-9A-Z]{16}\b")]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"\bgh[pousr]_[A-Za-z0-9]{36,255}\b")]
    private static partial Regex GitHubTokenRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{4,}\b")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"(?:password|pwd)\s*=\s*(?<value>[^;]{4,})", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringPasswordRegex();

    [GeneratedRegex(@"(?:password|passwd|pwd|secret|token|api[_-]?key|credential|private[_-]?key|access[_-]?key)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretKeyNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9+/=_.\-]+$")]
    private static partial Regex TokenCharsetRegex();

    [GeneratedRegex(@"^(?:\$\{.*\}|\{\{.*\}\}|<[^>]*>|%[^%]+%)$")]
    private static partial Regex PlaceholderPatternRegex();
}
