# Baseline File Design for Legacy Adoption

- Status: accepted
- Date: 2026-07-03

## Context and Problem Statement

A legacy codebase adopting ConfigLens may start with hundreds of findings. If the first CI run fails on all of them, the tool gets removed the same day. Adoption must be painless: accept the status quo, fail only on *new* findings. How are "known" findings identified and stored?

## Decision Drivers

- Baseline matching must survive unrelated edits (line shifts, file reordering).
- The file must be reviewable in a PR (small, diff-friendly, deterministic).
- No information leak: baselines are committed, so they must not contain secrets.

## Considered Options

1. Fingerprints: hash of (rule ID, file path, message), stored as a sorted JSON list.
2. Full finding snapshots (rule, file, line, message) with fuzzy line matching.
3. Suppression comments in the config files themselves.

## Decision Outcome

Chosen option 1: each finding gets a 16-hex-character fingerprint — a truncated SHA-256 of `ruleId|filePath|message`. The line number is deliberately excluded, so edits that shift lines do not invalidate the baseline. The baseline file is versioned JSON:

```json
{
  "version": 1,
  "fingerprints": ["0a1b2c3d4e5f6071", "…"]
}
```

Fingerprints are sorted for stable diffs. `BaselineFilter` splits scan results into new and suppressed findings; suppressed ones are reported separately and never fail the build. Secret values never enter the baseline because finding messages are already redacted (CL004).

Trade-off accepted: changing a finding's *message* (e.g. a renamed key or a reworded rule) invalidates its fingerprint and resurfaces the finding. That is tolerable — message changes are rare and resurfacing errs on the loud side. Suppression comments were rejected because they mutate the files being audited.

### Consequences

- Good: `--baseline` makes the first CI run green on any codebase; only regressions fail.
- Good: the file is small, sorted, versioned and reviewable.
- Bad: a fingerprint list is opaque to humans; you cannot tell from the file *what* is suppressed without re-running the scan.
