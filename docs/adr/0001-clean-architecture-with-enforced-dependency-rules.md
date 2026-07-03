# Clean Architecture with Enforced Dependency Rules

- Status: accepted
- Date: 2026-07-03

## Context and Problem Statement

ConfigLens combines several concerns that evolve at very different speeds: a stable domain model (findings, rules, severities), analysis logic (rule engine, scoring), and volatile technical adapters (Roslyn, JSON parsing, report renderers, CLI framework). How do we structure the solution so the core stays testable and independent of heavy dependencies like Roslyn, and so new scanners or renderers can be added without touching existing code?

## Decision Drivers

- The rule engine and score calculator must be unit-testable without I/O or Roslyn.
- Roslyn and `MSBuildWorkspace` are heavyweight, version-sensitive dependencies that must not leak into the core.
- Adding a new config source (YAML, env files) or report format must not modify existing code (Open/Closed).
- Architecture claims in a portfolio project must be verifiable, not aspirational.

## Considered Options

1. Clean Architecture layers (`Domain` → `Application` → `Infrastructure` → `Cli`) with dependency rules enforced by NetArchTest in CI.
2. Single project with folder-based separation.
3. Two projects (core + CLI) without enforced boundaries.

## Decision Outcome

Chosen option 1: four projects with strictly inward-pointing dependencies, enforced by architecture tests that fail the build.

- `ConfigLens.Domain` — findings, rules, severities, confidence levels. References nothing.
- `ConfigLens.Application` — scan orchestration, rule engine, scoring, ports (interfaces) for scanners and renderers. References only `Domain`.
- `ConfigLens.Infrastructure` — adapters: config scanners, Roslyn usage scanner, report renderers. Roslyn types live exclusively here.
- `ConfigLens.Cli` — composition root, command-line surface.

Scanners implement a common `IScanner` port and renderers an `IReportRenderer` port; both are registered via DI (strategy pattern), so extension happens by adding classes, not modifying them.

### Consequences

- Good: the core (Domain + Application) is pure C# with zero dependencies, enabling fast, deterministic unit tests and the ≥ 90 % coverage target.
- Good: dependency violations are caught by `ConfigLens.Architecture.Tests` in CI, not in code review.
- Good: swapping or adding adapters (YAML scanner, new report format) never touches the core.
- Bad: more projects and indirection than a small tool strictly needs; mapping between layer models adds some ceremony.
- Accepted trade-off: the ceremony is justified because extensibility across config sources and output formats is a core product promise, not speculative generality.
