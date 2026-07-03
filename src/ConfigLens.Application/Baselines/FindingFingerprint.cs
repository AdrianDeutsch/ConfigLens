using System.Security.Cryptography;
using System.Text;
using ConfigLens.Domain;

namespace ConfigLens.Application.Baselines;

/// <summary>
/// Stable identity of a finding for baseline matching (ADR-0006): rule ID,
/// file path and message — deliberately without the line number, so unrelated
/// edits that shift lines do not invalidate the baseline.
/// </summary>
public static class FindingFingerprint
{
    /// <summary>Computes the fingerprint of a finding.</summary>
    /// <param name="finding">The finding to fingerprint.</param>
    public static string Of(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var identity = $"{finding.RuleId}|{finding.Location.FilePath}|{finding.Message}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexStringLower(hash)[..16];
    }
}
