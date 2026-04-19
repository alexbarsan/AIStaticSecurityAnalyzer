# AI Static Security Analyzer

AI Static Security Analyzer is a modular C#/.NET dissertation project for static application security testing (SAST). It analyzes C# source code with Roslyn, enriches findings with CWE/CVE/CVSS data, scores findings with ML.NET, exports JSON and SARIF, and can act as a CI/CD security gate.

## Current Status

The repository already contains a working vertical slice:

- `Analyzer.CLI` orchestrates scans, NVD sync, AI training, JSON export, and SARIF export.
- `Analyzer.Roslyn` contains two implemented rules:
  - weak hashing (`CWE-327`)
  - hardcoded secrets (`CWE-798`)
- `Analyzer.CVE` stores NVD data in SQLite and enriches findings by CWE.
- `Analyzer.AI` trains and loads an ML.NET model for confidence scoring.
- `Analyzer.Reporting` writes JSON, SARIF, and CSV training exports.
- `.github/workflows/code-scanning.yaml` uploads SARIF to GitHub Code Scanning.

The codebase is usable, but it is still in a prototype stage. The most important next step is not only "more rules"; it is to harden the scan engine, tests, configuration, and reporting so later rules are reliable and thesis-grade.

## Architecture

- `Analyzer.CLI`
  - Console entry point and command orchestration
- `Analyzer.Core`
  - Domain models and core interfaces
- `Analyzer.Roslyn`
  - Roslyn-based analysis engine and security rules
- `Analyzer.CVE`
  - NVD API client, SQLite storage, finding enrichment
- `Analyzer.AI`
  - ML.NET training and inference
- `Analyzer.Reporting`
  - JSON, SARIF, and CSV export
- `Analyzer.Tests`
  - focused regression test runner for CSV dataset/export behavior
- `TempForTests`
  - Ad-hoc sample input, not a real automated test project

## Confirmed Repository Reality

This README reflects the current repository, not only the older context notes:

- The projects currently target `net9.0`.
- There is now a small automated test runner, but coverage is still narrow.
- Rule registration is hardcoded inside `RoslynCodeAnalyzer`.
- CLI argument parsing is custom and centralized in `Program.cs`.
- Semantic analysis currently uses a minimal set of metadata references, so real-world project resolution is still limited.

## Recommended Priority

The proposed implementation order is documented in:

- [Project Context](docs/CODEX_CONTEXT_V2.md)
- [User Manual](docs/USER_MANUAL.md)
- [Feature 01](docs/features/01-project-aware-scan-engine.md) through [Feature 10](docs/features/10-ai-dataset-and-confidence-pipeline-v2.md)

The short version:

1. Make scanning project-aware and deterministic.
2. Add automated tests and rule fixtures.
3. Implement SQL injection with basic taint tracking.
4. Extend the taint engine to additional vulnerability classes.
5. Add configuration, suppressions, better reporting, and stronger AI data handling.

## Prerequisites

- Windows with Visual Studio 2022 or the .NET SDK
- Current repository target: `.NET 9 SDK`
- Optional: `NVD_API_KEY` for better NVD API rate limits

If you want a more stable dissertation baseline, moving the solution to `.NET 8 LTS` is a valid next change. Right now the code and package references are aligned with `.NET 9`.

## Quick Start

### Run a local scan

```powershell
dotnet run --project Analyzer.CLI -- . --json
```

### Export SARIF

```powershell
dotnet run --project Analyzer.CLI -- . --sarif analysis.sarif.json
```

### Use AI scoring

```powershell
dotnet run --project Analyzer.CLI -- . --ai --min-confidence 0.70
```

### Fail the pipeline on severity

```powershell
dotnet run --project Analyzer.CLI -- . --fail-on high
```

### Sync NVD data

```powershell
dotnet run --project Analyzer.CLI -- sync-nvd --days 7
```

### Train the AI model

```powershell
dotnet run --project Analyzer.CLI -- train-ai
```

This now trains from the canonical labeled dataset:

- `Analyzer.AI/Training/training-labeled.csv`

## Outputs

- `analysis-report.json`
  - JSON report with summary and findings
- `analysis.sarif.json`
  - SARIF 2.1.0 output for GitHub Code Scanning
- `cves.db`
  - Local SQLite cache for NVD data
- `ai-model.zip`
  - Saved ML.NET model
- `Analyzer.AI/Training/training-labeled.csv`
  - canonical labeled dataset used for training
- `Analyzer.AI/Training/training-candidates.csv`
  - unlabeled candidate rows exported for manual review and labeling

## Known Gaps

These are important and should be treated as real backlog items:

- automated tests exist only for a narrow CSV/dataset slice
- no project-aware scan loading from `.sln` / `.csproj`
- only two implemented rules
- limited semantic references for analysis
- no suppression or baseline system
- AI training/export workflow needs stronger dataset hygiene
- some context documentation in the repository is now outdated relative to the code

## Documentation

- [docs/CODEX_CONTEXT_V2.md](docs/CODEX_CONTEXT_V2.md)
- [docs/USER_MANUAL.md](docs/USER_MANUAL.md)
- [docs/features/01-project-aware-scan-engine.md](docs/features/01-project-aware-scan-engine.md)
- [docs/features/02-automated-test-harness.md](docs/features/02-automated-test-harness.md)
- [docs/features/03-sql-injection-taint-analysis.md](docs/features/03-sql-injection-taint-analysis.md)
- [docs/features/04-command-injection-rule.md](docs/features/04-command-injection-rule.md)
- [docs/features/05-path-traversal-rule.md](docs/features/05-path-traversal-rule.md)
- [docs/features/06-insecure-deserialization-rule.md](docs/features/06-insecure-deserialization-rule.md)
- [docs/features/07-xxe-and-unsafe-xml-rule.md](docs/features/07-xxe-and-unsafe-xml-rule.md)
- [docs/features/08-configuration-suppressions-and-baseline.md](docs/features/08-configuration-suppressions-and-baseline.md)
- [docs/features/09-reporting-fingerprints-and-sarif-v2.md](docs/features/09-reporting-fingerprints-and-sarif-v2.md)
- [docs/features/10-ai-dataset-and-confidence-pipeline-v2.md](docs/features/10-ai-dataset-and-confidence-pipeline-v2.md)
