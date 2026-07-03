# Report Formats and the Stable JSON Schema Contract

- Status: accepted
- Date: 2026-07-04

## Context and Problem Statement

Scan results are consumed by four very different audiences: humans at a terminal, CI pipelines, code scanning platforms, and people reading an attached report file. Which formats does ConfigLens emit, and what stability do consumers get?

## Decision Drivers

- CI scripts parse the output; renaming a field breaks pipelines silently.
- GitHub code scanning requires SARIF 2.1.0.
- Report files get attached to tickets — they must work without a web server or external assets.

## Considered Options

1. Four renderers behind one `IReportRenderer` port: Console (Spectre), JSON (versioned schema), HTML (self-contained), SARIF 2.1.0.
2. JSON only, with external converters for everything else.
3. One flexible templating engine.

## Decision Outcome

Chosen option 1. Contracts per format:

- **Console** — human-facing, *not* a contract: layout and coloring may change any time. Scripts must use JSON.
- **JSON** — the machine contract. `schemaVersion` marks the format (currently 1). Fields are only ever *added*; renaming or removing a field, or changing a field's meaning, requires bumping `schemaVersion` and a new ADR. Findings carry their baseline fingerprint so consumers can build suppressions without re-hashing.
- **SARIF 2.1.0** — for the GitHub Security tab. Severity maps to `error`/`warning`/`note`; confidence travels in `properties.confidence`; fingerprints go to `partialFingerprints`.
- **HTML** — one self-contained file, inline CSS, no scripts, no external requests; all content HTML-encoded.

Snapshot tests (Verify) pin all three file formats; any output change is an explicit, reviewed diff of the committed `*.verified.*` files.

The CLI surface belongs to the same contract: command names, option names, defaults and the exit codes (0 clean, 1 findings at/above `--fail-on`, 2 tool error — including argument errors) are stable from v0.1 on.

### Consequences

- Good: pipelines can rely on `--format json` + exit codes; security tooling gets native SARIF.
- Good: adding a format (e.g. Markdown for PR comments in M5) is a new class behind the port, not a change.
- Bad: schema stability means design mistakes in v0.1 field naming live until a major version.
