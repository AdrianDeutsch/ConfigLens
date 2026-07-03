# Security Policy

## Supported versions

Only the latest released minor version receives security fixes.

## Reporting a vulnerability

Please **do not** open a public issue for security vulnerabilities.

Use [GitHub private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) on this repository to report privately.

You can expect an initial response within 7 days. Once a fix is available, the vulnerability will be disclosed in the release notes with credit to the reporter (unless you prefer to remain anonymous).

## Scope notes

ConfigLens analyzes configuration files that may contain secrets. The tool itself never transmits scanned content anywhere — all analysis is local. Findings (including secret detections) end up in the reports you generate; treat generated reports with the same care as the configuration files themselves.
