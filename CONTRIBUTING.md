# Contributing to ConfigLens

Thanks for your interest in contributing!

## Prerequisites

- .NET SDK as pinned in [`global.json`](global.json)

## Getting started

```bash
git clone https://github.com/adriandeutsch/ConfigLens.git
cd ConfigLens
dotnet build ConfigLens.slnx
dotnet test ConfigLens.slnx
```

## Ground rules

- **Tests accompany code.** Every PR that changes behavior includes tests for the new behavior. Every bug fix includes a regression test.
- **Conventional Commits.** Use `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:` prefixes.
- **Architecture rules are enforced.** `Domain` references nothing, `Application` references only `Domain`, Roslyn types stay in `Infrastructure`. The architecture test suite fails the build on violations.
- **Formatting.** Run `dotnet format ConfigLens.slnx` before pushing; CI verifies with `--verify-no-changes`.
- **Design decisions** that affect architecture or public contracts are recorded as ADRs (MADR format) in `docs/adr/`.
- **Warnings are errors.** The build treats all warnings as errors; do not suppress analyzers without a justifying comment.

## Proposing a new rule

Open an issue describing: what the rule detects, why it matters in production, the proposed severity and confidence behavior, and example code/config that triggers it.
