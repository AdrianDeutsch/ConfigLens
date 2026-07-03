# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `configlens scan` CLI (M4): `--environments`, `--format console|json|html|sarif` (multiple allowed), `--output`, `--fail-on error|warning|none`, `--baseline` and `--write-baseline`; stable exit codes (0 clean, 1 findings at/above threshold, 2 tool error).
- Report renderers behind one port: rich Spectre console output with per-rule breakdown and score panel, versioned JSON schema, self-contained HTML, SARIF 2.1.0 for the GitHub Security tab (ADR-0004); all file formats pinned by Verify snapshot tests.
- Cross-referencing rules (M3): `CL001` missing key, `CL003` dead configuration, `CL005` type mismatch against options properties, `CL006` unbound options class, `CL007` typo suspicion via edit distance.
- Config Health Score: 100 minus severity-weighted, confidence-scaled penalties, floored at 0 (ADR-0005); invariants pinned with property-based tests.
- Baseline support: fingerprint-based suppression of known findings for painless legacy adoption (ADR-0006).
- `GetConnectionString(...)` is recognized as a read of `ConnectionStrings:*`.
- Roslyn usage scanner: finds `IConfiguration` indexer/`GetValue`/`GetSection` reads and `IOptions<T>` bindings with full semantic analysis, composes `GetSection` chains, and degrades to syntax-only analysis with Low confidence when a project does not compile (ADR-0003) (M2).
- `CL900` unresolvable key access: dynamic keys and sections of unknown origin produce informational notes instead of guessed usages.
- JSON configuration scanner: discovers `appsettings*.json` files, builds a per-environment model with hierarchical keys and line numbers (M1).
- `CL002` environment drift rule: keys introduced by one environment file but missing from another environment's effective configuration.
- `CL004` hardcoded secret rule: token format patterns, secret-suggesting key names and Shannon-entropy analysis, with honest confidence levels (ADR-0002).
- Fixture suite (`CleanApp`, `DriftApp`, `SecretsApp`) with full-pipeline integration tests.
- Solution skeleton: Clean Architecture layout (`Domain`, `Application`, `Infrastructure`, `Cli`), test projects, architecture tests enforcing dependency rules (M0).
- CI pipeline building and testing on Linux and Windows, formatting verification, tool packaging.
