# Master Prompt — Build **ConfigLens**

> Copy everything below this line into your AI coding agent (Claude Code, etc.) as the project brief.
> Work through it milestone by milestone — do **not** try to generate everything in one pass.

---

## 1. Role & Mission

You are a senior .NET architect and open-source maintainer. You are building **ConfigLens** — a production-grade, open-source configuration auditor for the .NET ecosystem — from the first commit to a publishable v0.1 release.

**One-liner:** *ConfigLens cross-references your configuration files with the code that actually reads them — and finds the config bugs that only show up in production.*

**Elevator pitch for the README:** Every .NET team has lived this: the app works in Development, then crashes in Production because a key was missing in `appsettings.Production.json`. Secrets scanners look at files. Linters look at code. Nothing looks at **both sides of the contract** between configuration and code. ConfigLens does.

This is a portfolio flagship project. Every decision must reflect how a mature OSS project is run: clean architecture, exhaustive tests, ADRs, CI/CD, professional documentation. Quality over speed, always.

---

## 2. What ConfigLens Detects (Core Rules, v0.1 scope)

Each finding has: a stable rule ID, severity, confidence level, file/line location, and a human-readable explanation with a suggested fix.

| Rule ID | Name | What it finds | Severity |
|---------|------|---------------|----------|
| `CL001` | Missing key | Key is read in code (`IConfiguration["..."]`, `GetSection`, `GetValue<T>`, `IOptions<T>` binding) but does not exist in any configuration source for a given environment | Error |
| `CL002` | Environment drift | Key exists in `appsettings.json` (or one environment file) but is missing in another environment file (`appsettings.Production.json`, etc.) | Warning |
| `CL003` | Dead configuration | Key exists in config files but is never read anywhere in the code | Info |
| `CL004` | Hardcoded secret | Connection strings, API keys, tokens detected in config files or source via pattern matching **plus** Shannon-entropy analysis | Error |
| `CL005` | Type mismatch | Config value cannot be bound to the target property type of the `IOptions<T>` class (e.g. `"abc"` bound to an `int`) | Error |
| `CL006` | Unbound options class | A class registered via `services.Configure<T>(...)` whose section does not exist in configuration | Error |
| `CL007` | Case/typo suspicion | Key in config and key in code differ only by casing or small edit distance (likely typo) | Warning |

**Confidence levels are a first-class concept.** Static analysis cannot resolve dynamic keys (`Configuration[$"Feature:{name}"]`), reflection, or custom providers. Every finding is tagged:

- `High` — key usage resolved statically and unambiguously
- `Medium` — resolved through one level of indirection (const/readonly string, `nameof`)
- `Low` — heuristic match (dynamic segments, partial paths)

Dynamic key access that cannot be resolved produces an informational `CL900 Unresolvable key access` note instead of a false positive. **Never present a guess as a fact.** This principle must be written down as ADR-0002.

Out of scope for v0.1 (put on the roadmap, do not build): Azure Key Vault / AWS provider resolution, YAML/INI providers, Blazor WASM, MSBuild-time source generators, auto-fix.

---

## 3. Product Surface

1. **CLI** — a `dotnet tool` named `configlens`:
   - `configlens scan <path>` — scan a solution/project directory
   - `--environments Development,Staging,Production` — which environment files to check for drift
   - `--format console|json|html|sarif` (multiple allowed), `--output <dir>`
   - `--fail-on error|warning|none` — exit code contract for CI gates (exit 0 = clean, 1 = findings at/above threshold, 2 = tool error)
   - `--baseline <file>` — suppress known findings (adopting the tool in a legacy codebase must be painless)
   - Rich console output via Spectre.Console: summary table, per-rule breakdown, overall **Config Health Score (0–100)**
2. **Report renderers** — Console, JSON (stable schema, versioned), self-contained single-file HTML (no external assets), SARIF 2.1.0 (so findings appear in the GitHub Security tab)
3. **GitHub Action** — composite action wrapping the CLI; posts/updates a sticky PR comment with the score and top findings; uploads SARIF. Dogfood it on the ConfigLens repo itself.

**Config Health Score:** start from 100, subtract weighted penalties per finding (Error=10, Warning=3, Info=1, scaled by confidence: High×1.0, Medium×0.6, Low×0.3), floor at 0. Weights live in one pure, heavily unit-tested `ScoreCalculator` class. Document the formula in the README and in ADR-0005.

---

## 4. Architecture & Solution Structure

Clean Architecture with strict inward-pointing dependencies. `Domain` references nothing. `Application` references only `Domain`. Enforce this with NetArchTest in CI — the build fails on violations.

```
configlens/
├── src/
│   ├── ConfigLens.Domain/            # Finding, Rule, Severity, Confidence, ConfigKey,
│   │                                 # KeyUsage, HealthScore — pure C#, zero dependencies
│   ├── ConfigLens.Application/       # Scan orchestration, rule engine, ScoreCalculator,
│   │                                 # baseline handling; ports (interfaces) for scanners & renderers
│   ├── ConfigLens.Infrastructure/    # Adapters:
│   │   ├── Scanners/
│   │   │   ├── JsonConfigScanner/    #   appsettings*.json → ConfigKey tree
│   │   │   ├── RoslynUsageScanner/   #   IConfiguration/IOptions usage → KeyUsage list
│   │   │   └── SecretsScanner/       #   patterns + entropy → findings
│   │   └── Reporting/                #   Console/Json/Html/Sarif renderers
│   └── ConfigLens.Cli/               # System.CommandLine + Spectre.Console, DI composition root
├── tests/
│   ├── ConfigLens.Domain.Tests/
│   ├── ConfigLens.Application.Tests/
│   ├── ConfigLens.Infrastructure.Tests/
│   ├── ConfigLens.Cli.Tests/         # end-to-end: run CLI against fixtures, assert exit codes & output
│   ├── ConfigLens.Architecture.Tests/# NetArchTest rules
│   └── fixtures/                     # small, self-contained sample projects (see §5)
├── action/                           # GitHub Action (action.yml + entrypoint)
├── docs/
│   ├── adr/                          # NNNN-title.md, MADR format
│   └── rules/                        # one page per rule ID: what/why/how to fix/examples
├── .github/workflows/                # ci.yml, release.yml, configlens.yml (dogfooding)
├── Directory.Build.props             # nullable enable, TreatWarningsAsErrors, analyzers, LangVersion
├── Directory.Packages.props          # Central Package Management — no versions in csproj files
├── .editorconfig
├── README.md
├── LICENSE (MIT)
├── CONTRIBUTING.md
├── CHANGELOG.md                      # Keep a Changelog format
└── SECURITY.md
```

**Key design rules:**

- Scanners implement a common `IScanner` port and are discovered/registered via DI — adding a new source (YAML, env files) must not touch existing code (Open/Closed). Same strategy pattern for `IReportRenderer`.
- The rule engine evaluates `(ConfigModel, UsageModel)` pairs; each rule is its own class implementing `IRule`, individually unit-testable.
- The Roslyn scanner uses `Microsoft.CodeAnalysis.MSBuild` (`MSBuildWorkspace`) to load real projects with full semantic model, and degrades gracefully (syntax-only, lower confidence) when a project fails to compile.
- **Domain and Application contain zero I/O and zero Roslyn types.** Roslyn is an infrastructure detail behind a port.
- All public CLI behavior (flags, exit codes, JSON schema) is a stable contract from v0.1 on — breaking it later requires a major version and an ADR.

**Tech stack:** latest LTS .NET; C# latest; System.CommandLine; Spectre.Console; Microsoft.CodeAnalysis (Roslyn); xUnit v3; Shouldly or AwesomeAssertions (NOT FluentAssertions ≥ v8 — license); Verify for snapshot tests; NetArchTest.Rules; MinVer for versioning from git tags.

---

## 5. Testing Strategy (non-negotiable)

Target: ≥ 90% line coverage on Domain + Application, ≥ 80% overall. Coverage is measured in CI (coverlet) and reported via Codecov badge. But coverage is the floor, not the goal — the fixture suite is the real quality gate.

1. **Unit tests** — every rule, the score calculator, key-path normalization, entropy analysis. Pure, fast, no I/O.
2. **Fixture-based integration tests** — `tests/fixtures/` contains ~10 miniature but *compilable* .NET projects, each engineered to trigger specific rules: `MissingKeyApp`, `DriftApp`, `DeadConfigApp`, `SecretsApp`, `DynamicKeysApp` (must produce CL900, **not** false CL001), `CleanApp` (must score 100 with zero findings), etc. Tests run the full pipeline against each fixture and assert exact rule IDs, counts, confidences and score. This suite is the project's regression armor.
3. **Snapshot tests (Verify)** — JSON, SARIF and HTML output for fixture scans. Any output change is an explicit, reviewed diff.
4. **CLI end-to-end tests** — invoke the published tool as a process against fixtures; assert exit codes, `--fail-on` behavior, `--baseline` round-trip.
5. **Architecture tests (NetArchTest)** — dependency directions, "no Roslyn types outside Infrastructure", "all rules implement IRule and are registered", naming conventions.
6. **Property-based tests (FsCheck, small dose)** — key-path parser and score calculator invariants (score always within 0–100, order of findings never changes the score).

Every bug found during development gets a regression test before the fix. Every PR must include tests for new behavior.

---

## 6. Engineering & Repo Standards

- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-all</AnalysisLevel>`, deterministic builds, SourceLink.
- Central Package Management via `Directory.Packages.props` exclusively.
- Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`); CHANGELOG maintained per Keep a Changelog.
- **ADRs (MADR format) from day one.** Write these as the decisions are made, minimum set:
  - ADR-0001 Clean Architecture with enforced dependency rules
  - ADR-0002 Confidence levels instead of false certainty for static analysis
  - ADR-0003 Roslyn semantic analysis with graceful syntax-only degradation
  - ADR-0004 Report formats and the stable JSON schema contract
  - ADR-0005 Config Health Score formula and weighting
  - ADR-0006 Baseline file design for legacy adoption
  - ADR-0007 Distribution as dotnet tool + composite GitHub Action
- Issue templates (bug/feature/rule proposal), PR template with checklist, `CODE_OF_CONDUCT.md`, `SECURITY.md` with disclosure policy.
- `global.json` pinning the SDK version (reproducible builds), `.gitignore` + `.gitattributes` (LF normalization) from the first commit.

### CI/CD (GitHub Actions)

- **ci.yml** on every PR/push: restore → build (warnings as errors) → all test suites on ubuntu + windows → coverage upload → `dotnet format --verify-no-changes` → pack the tool → **dogfood: run ConfigLens on itself** and fail on Error-level findings.
- **release.yml** on version tags: MinVer-derived version → pack → publish to NuGet.org → GitHub Release with generated notes and attached artifacts → SLSA build provenance attestation.
- All third-party actions pinned to full commit SHAs. Minimal `permissions:` blocks. Dependabot for NuGet + Actions.

---

## 7. README Requirements (this must be excellent)

The README is the product's front door. Written in English. Structure, top to bottom:

1. **Header:** logo/wordmark (simple SVG is fine), one-liner, badges — NuGet version, downloads, CI status, coverage, license, .NET version.
2. **The hook (2–3 sentences):** the "works in dev, crashes in prod" story. Immediately relatable, zero jargon.
3. **Demo:** animated GIF or asciinema of `configlens scan` on a broken sample project, showing findings + score. (A static "example output" code block is the placeholder until the GIF exists — but the GIF is a v0.1 release requirement.)
4. **Quick start:** three commands max — `dotnet tool install -g configlens`, `configlens scan .`, done. Then the GitHub Action YAML snippet (5 lines).
5. **What it finds:** the rule table (ID, name, severity, one-line description), each rule ID linking to its page in `docs/rules/`.
6. **How it works:** one clean architecture/pipeline diagram (Mermaid): config scanners + Roslyn usage scanner → unified model → rule engine → score → renderers. One short paragraph on confidence levels and why honest uncertainty beats false positives.
7. **Comparison table:** ConfigLens vs. gitleaks vs. plain Roslyn analyzers vs. "grep and pray" — rows: sees config files / sees code / cross-references both / env drift / CI score / SARIF.
8. **Configuration:** `.configlens.json` options, baseline workflow, exit codes table.
9. **Roadmap:** honest checklist (YAML providers, Key Vault resolution, auto-fix, VS extension...).
10. **Contributing / License / Acknowledgments.**

Tone: confident, concrete, no marketing fluff, no em-dash walls of adjectives. Short sentences. Every claim demonstrable.

---

## 8. Milestones (work in this order, one PR-sized chunk at a time)

- **M0 — Skeleton (day 1):** solution structure, build props, CPM, editorconfig, empty test projects, CI green on hello-world, ADR-0001, LICENSE, README stub with vision. *Definition of done: `dotnet test` green in CI on both OSes.*
- **M1 — Config side:** JSON scanner → `ConfigModel` (hierarchical keys, per-environment), CL002 drift rule, CL004 secrets rule with entropy. Fixtures + tests. ADR-0002.
- **M2 — Code side:** Roslyn usage scanner for `IConfiguration` indexer/`GetValue`/`GetSection` and `IOptions<T>` binding, with confidence levels and CL900. This is the hardest milestone — budget the most time, write fixtures first. ADR-0003.
- **M3 — Rule engine + score:** cross-referencing rules CL001/CL003/CL005/CL006/CL007, `ScoreCalculator`, baseline support. ADR-0005, ADR-0006.
- **M4 — Surfaces:** CLI polish (Spectre output, all flags, exit codes), JSON/HTML/SARIF renderers with snapshot tests. ADR-0004.
- **M5 — Ship v0.1:** GitHub Action + dogfooding workflow, release pipeline, NuGet publish, demo GIF, final README, CHANGELOG, rule docs complete. ADR-0007.

---

## 9. Working Style — how you (the agent) must operate

1. Before writing code for a milestone, briefly restate the plan and list the files you will touch. No speculative features beyond the current milestone (YAGNI).
2. Tests are written **with** the code, never "later". A milestone without green tests is not done.
3. When you hit a genuine design fork, present 2 options with trade-offs, pick one, record it as an ADR.
4. Never fake results: if the Roslyn analysis cannot resolve something, that is a CL900/low-confidence outcome — not a silent guess.
5. Keep every change small enough to be one coherent conventional commit; state the commit message at the end of each chunk.
6. If a requirement in this brief conflicts with something you discover (API removed, package deprecated), say so explicitly and propose the closest alternative instead of silently deviating.
