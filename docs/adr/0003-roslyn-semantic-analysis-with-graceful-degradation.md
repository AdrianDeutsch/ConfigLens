# Roslyn Semantic Analysis with Graceful Syntax-Only Degradation

- Status: accepted
- Date: 2026-07-03

## Context and Problem Statement

Finding configuration reads in code requires knowing the type of the receiver: `config["Smtp:Host"]` is a configuration access, `dictionary["Smtp:Host"]` is not. Full type information requires compiling the project — but ConfigLens must also produce useful results for projects that do not compile (missing SDK workload, broken reference, scanning a partial checkout). How do we analyze the code side reliably without making a successful build a prerequisite?

## Decision Drivers

- Type-based classification eliminates whole categories of false positives.
- A CI tool that gives up when the build is broken is useless exactly when it is needed most.
- Confidence levels (ADR-0002) give us a vocabulary for weaker analysis results.

## Considered Options

1. Semantic analysis via `MSBuildWorkspace` with automatic fallback to syntax-only pattern matching per project.
2. Semantic analysis only; fail the scan when a project cannot be compiled.
3. Syntax-only analysis everywhere (no compilation at all).

## Decision Outcome

Chosen option 1, implemented as two analyzers behind one scanner:

- **Semantic path** (`ConfigUsageAnalyzer`): projects are loaded with `MSBuildWorkspace`; receivers are classified by their type symbols against the `Microsoft.Extensions.Configuration` abstractions. Literal keys are High confidence; one level of indirection (const field, `nameof`, constant folding, readonly field with literal initializer) is Medium. `GetSection` chains are composed into absolute paths. Anything else — dynamic keys, sections of unknown origin — becomes a CL900 note, never a guessed key.
- **Degraded path** (`SyntaxOnlyUsageAnalyzer`): used automatically when a project fails to load or its compilation lacks the configuration abstractions. Pure syntax patterns (indexer on config-named receivers, `GetSection`/`GetValue` by method name); everything it finds is Low confidence because types are only guessed.

The scanner decides per project, so one broken project degrades only itself, not the scan.

Further scope decisions for v0.1:

- No data-flow analysis: a key stored in a local variable before use is CL900. Honest over clever.
- A receiver statically typed `IConfiguration` is treated as the configuration root; a receiver typed `IConfigurationSection` that is not a visible `GetSection` chain is unresolvable (unknown absolute path).
- Analysis is per document; cross-method or cross-project resolution is out of scope.

### Consequences

- Good: no false positives from dictionaries/indexers of other types; broken builds still get a useful (if weaker) scan.
- Good: the confidence attached to each usage tells downstream rules exactly how much to trust it.
- Bad: two analyzers to maintain; the syntax-only path will miss renamed receivers and produce occasional false usages.
- Bad: `MSBuildWorkspace` requires an installed SDK and restored projects for the semantic path; this is documented as a CLI requirement.
