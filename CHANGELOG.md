# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- JSON configuration scanner: discovers `appsettings*.json` files, builds a per-environment model with hierarchical keys and line numbers (M1).
- `CL002` environment drift rule: keys introduced by one environment file but missing from another environment's effective configuration.
- `CL004` hardcoded secret rule: token format patterns, secret-suggesting key names and Shannon-entropy analysis, with honest confidence levels (ADR-0002).
- Fixture suite (`CleanApp`, `DriftApp`, `SecretsApp`) with full-pipeline integration tests.
- Solution skeleton: Clean Architecture layout (`Domain`, `Application`, `Infrastructure`, `Cli`), test projects, architecture tests enforcing dependency rules (M0).
- CI pipeline building and testing on Linux and Windows, formatting verification, tool packaging.
