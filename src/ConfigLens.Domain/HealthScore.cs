namespace ConfigLens.Domain;

/// <summary>
/// The Config Health Score: 100 minus weighted penalties per finding,
/// floored at 0 (see ADR-0005 for the formula).
/// </summary>
/// <param name="Value">Score between 0 and 100.</param>
/// <param name="TotalPenalty">Unfloored sum of all penalties, for reporting.</param>
public sealed record HealthScore(int Value, double TotalPenalty)
{
    /// <summary>Score of a scan without findings.</summary>
    public static readonly HealthScore Perfect = new(100, 0);
}
