# Config Health Score Formula and Weighting

- Status: accepted
- Date: 2026-07-03

## Context and Problem Statement

A list of findings does not answer the question teams actually ask: "how bad is it, and are we getting better?" ConfigLens needs one number that is comparable across scans, moves in the right direction when findings change, and can gate CI. How is that number computed?

## Decision Drivers

- The score must be explainable in one sentence and reproducible by hand.
- Uncertain findings (ADR-0002) must hurt less than certain ones.
- Adding a finding must never improve the score (monotonicity).

## Considered Options

1. Penalty subtraction: start at 100, subtract weighted penalties, floor at 0.
2. Ratio-based: percentage of "clean" keys over all keys.
3. Letter grades (A–F) from thresholds.

## Decision Outcome

Chosen option 1:

```
score = max(0, round(100 − Σ penalty(finding)))
penalty = severityWeight × confidenceFactor
severityWeight:    Error = 10, Warning = 3, Info = 1
confidenceFactor:  High = 1.0, Medium = 0.6, Low = 0.3
```

Rounding is midpoint-away-from-zero. The weights live in one pure `ScoreCalculator` class; property-based tests pin the invariants (0 ≤ score ≤ 100, order independence, monotonicity).

Ratio-based scores were rejected because the denominator is unstable (adding config keys changes the score without any finding changing); grades were rejected because they hide movement within a band.

### Consequences

- Good: one High-confidence error costs 10 points — a project with ten real errors scores 0, which matches intuition.
- Good: confidence scaling propagates honest uncertainty into the number (a Low-confidence info costs 0.3).
- Bad: absolute weights are opinion, not science; changing them later shifts every dashboard. They are therefore a documented contract from v0.1 on, and changes require a new ADR.
