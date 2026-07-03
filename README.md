# ConfigLens

> **Status: pre-release scaffolding (milestone M0).** The architecture and pipeline described below are being built milestone by milestone — see the [roadmap](#roadmap).

**ConfigLens cross-references your configuration files with the code that actually reads them — and finds the config bugs that only show up in production.**

Every .NET team has lived this: the app works in Development, then crashes in Production because a key was missing in `appsettings.Production.json`. Secrets scanners look at files. Linters look at code. Nothing looks at **both sides of the contract** between configuration and code. ConfigLens does.

## What it will find

| Rule | Name | Severity | Description |
|------|------|----------|-------------|
| `CL001` | Missing key | Error | Key is read in code but missing from configuration for an environment |
| `CL002` | Environment drift | Warning | Key exists in one environment file but not in another |
| `CL003` | Dead configuration | Info | Key exists in config files but is never read by code |
| `CL004` | Hardcoded secret | Error | Secrets in config or source, via patterns plus entropy analysis |
| `CL005` | Type mismatch | Error | Config value cannot bind to the target `IOptions<T>` property type |
| `CL006` | Unbound options class | Error | `services.Configure<T>(...)` points at a section that does not exist |
| `CL007` | Case/typo suspicion | Warning | Config key and code key differ only by casing or small edit distance |

Static analysis cannot resolve everything. Every finding carries a **confidence level** (`High`/`Medium`/`Low`), and unresolvable dynamic key access is reported as an informational `CL900` note instead of a false positive. Honest uncertainty beats false certainty.

## Quick start (target UX)

```bash
dotnet tool install -g configlens
configlens scan .
```

## Roadmap

- [x] **M0** — Solution skeleton, Clean Architecture layout, CI on Linux + Windows
- [x] **M1** — JSON config scanner, environment drift (CL002), secrets detection (CL004)
- [x] **M2** — Roslyn usage scanner with confidence levels (CL900)
- [x] **M3** — Cross-referencing rules (CL001/003/005/006/007), Config Health Score, baselines
- [ ] **M4** — CLI polish, JSON/HTML/SARIF renderers
- [ ] **M5** — GitHub Action, NuGet release v0.1

Out of scope for v0.1: Azure Key Vault / AWS provider resolution, YAML/INI providers, Blazor WASM, MSBuild-time source generators, auto-fix.

## Architecture

Clean Architecture with strictly inward-pointing dependencies, enforced by NetArchTest in CI:

```
Domain  ←  Application  ←  Infrastructure (scanners, Roslyn, renderers)  ←  CLI
```

Design decisions are recorded as ADRs in [`docs/adr/`](docs/adr/).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Conventional Commits, tests with every change.

## License

[MIT](LICENSE)
