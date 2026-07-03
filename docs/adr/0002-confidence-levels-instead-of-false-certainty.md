# Confidence Levels Instead of False Certainty

- Status: accepted
- Date: 2026-07-03

## Context and Problem Statement

ConfigLens is a static analyzer. Static analysis cannot resolve everything it encounters: dynamic key access (`Configuration[$"Feature:{name}"]`), reflection, custom configuration providers, or values that merely look random. A tool that presents such guesses as facts erodes trust with the first false positive — and a config tool nobody trusts is a config tool nobody runs in CI. How do we report findings whose certainty varies by orders of magnitude?

## Decision Drivers

- False positives are the primary reason analyzers get disabled.
- Downstream consumers (score, CI gates, baselines) need a machine-readable notion of certainty.
- Some heuristics (entropy-based secret detection) are genuinely useful but inherently fuzzy.

## Considered Options

1. Confidence as a first-class property on every finding, with three levels and honest downgrade paths.
2. Report only what is certain, drop everything else.
3. Report everything at equal weight and let users filter.

## Decision Outcome

Chosen option 1: every `Finding` carries a `Confidence` value.

- `High` — resolved statically and unambiguously (exact token format, key usage via string literal).
- `Medium` — resolved through one level of indirection (const/readonly string, `nameof`, secret-suggesting key name).
- `Low` — heuristic match (entropy-only secret detection, dynamic segments, partial paths).

Two hard rules follow:

1. **A guess is never presented as a fact.** Detection logic must map weaker signals to lower confidence, visibly. The secrets rule (CL004) is the template: exact token formats are High, key-name heuristics are Medium, entropy alone is Low.
2. **Unresolvable is a category, not an error.** Key accesses the Roslyn scanner cannot resolve produce an informational `CL900 Unresolvable key access` note instead of a speculative CL001.

Confidence also scales the Config Health Score penalties (High ×1.0, Medium ×0.6, Low ×0.3, see ADR-0005 when written), so uncertainty propagates end to end instead of being flattened at the first layer.

### Consequences

- Good: heuristics can ship without poisoning trust; users can gate CI on High-confidence errors only.
- Good: the JSON/SARIF schema carries confidence, so downstream tooling can filter.
- Bad: every rule author must consciously decide a confidence per detection path; reviews must check for over-claiming.
- Bad: three levels are a simplification; we accept the loss of granularity for explainability.
