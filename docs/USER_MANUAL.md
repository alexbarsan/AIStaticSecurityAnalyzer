# User Manual - AI Static Security Analyzer

## Overview

AI Static Security Analyzer scans C# source code for security issues, enriches findings with NVD-based vulnerability information, and exports results in formats suitable for local review and CI/CD pipelines.

This manual describes how to run the current application in this repository.

## Prerequisites

- .NET 9 SDK installed
- Windows with Visual Studio 2022 is the most convenient local setup
- the current GitHub Actions workflow also runs on Ubuntu
- Optional: `NVD_API_KEY` environment variable for better NVD API rate limits

## Repository Layout

- `Analyzer.CLI`
  - application entry point
- `Analyzer.Core`
  - models and interfaces
- `Analyzer.Roslyn`
  - analyzer engine and current rules
- `Analyzer.CVE`
  - NVD integration and SQLite storage
- `Analyzer.AI`
  - ML.NET model training and scoring
- `Analyzer.Reporting`
  - JSON, SARIF, and CSV export

## Main Commands

### 1. Scan a directory

```powershell
dotnet run --project Analyzer.CLI -- . 
```

This scans the target path, prints findings to the console, enriches them from `cves.db`, and returns an exit code based on the highest severity found.

### 2. Export JSON

```powershell
dotnet run --project Analyzer.CLI -- . --json
```

Output:

- `analysis-report.json`

### 3. Export SARIF

```powershell
dotnet run --project Analyzer.CLI -- . --sarif analysis.sarif.json
```

Output:

- `analysis.sarif.json`

### 4. Use AI confidence scoring

```powershell
dotnet run --project Analyzer.CLI -- . --ai
```

If `ai-model.zip` exists, the analyzer loads it and updates finding confidence scores.

To filter weak-confidence results:

```powershell
dotnet run --project Analyzer.CLI -- . --ai --min-confidence 0.70
```

### 5. Enforce a security gate

```powershell
dotnet run --project Analyzer.CLI -- . --fail-on high
```

Supported values:

- `info`
- `low`
- `medium`
- `high`
- `critical`

Behavior:

- exit code `0` if no finding meets the threshold
- exit code `2` if any finding meets or exceeds the threshold

### 6. Sync NVD data

```powershell
dotnet run --project Analyzer.CLI -- sync-nvd --days 7
```

This populates or updates the local `cves.db` SQLite database with recently modified NVD records.

### 7. Train the AI model

```powershell
dotnet run --project Analyzer.CLI -- train-ai
```

Expected input:

- `Analyzer.AI/Training/training-data.csv`

Output:

- `ai-model.zip`

### 8. Export findings as ML training candidates

```powershell
dotnet run --project Analyzer.CLI -- . --export-training training-data.csv
```

Current behavior:

- relative exports are written under `Analyzer.AI/Training/`
- exported rows are candidate rows for later manual labeling

## Current Detection Coverage

### Weak hashing

- Detects MD5 and SHA1 usage
- Severity: High
- CWE: `CWE-327`

### Hardcoded secrets

- Detects suspicious string literals assigned to sensitive identifiers
- Severity: Critical
- CWE: `CWE-798`

## Typical Workflows

### Local development workflow

1. Run a scan against a local project.
2. Export JSON or SARIF if needed.
3. Review findings and false positives.
4. Export candidate training rows if you want to improve AI scoring later.

### CI/CD workflow

1. Restore and build the solution.
2. Optionally sync NVD data.
3. Run the analyzer with `--sarif`.
4. Upload SARIF to GitHub Code Scanning.
5. Run the analyzer again with `--fail-on` to enforce the security gate.

## Output Files

- `analysis-report.json`
  - structured scan results
- `analysis.sarif.json`
  - code scanning integration format
- `cves.db`
  - local vulnerability database cache
- `ai-model.zip`
  - trained ML.NET model

## Known Limitations

- the analyzer currently scans raw `.cs` files rather than loading full projects/solutions
- only two rules are implemented
- no automated test suite exists yet
- current AI workflow is useful for experimentation, but not yet mature enough for rigorous evaluation
- CVE enrichment is best-effort and not a direct proof that a detected pattern maps to one exact CVE

## Recommended Next Reading

- [README.md](../README.md)
- [docs/CODEX_CONTEXT_V2.md](CODEX_CONTEXT_V2.md)
- [docs/features/03-sql-injection-taint-analysis.md](features/03-sql-injection-taint-analysis.md)
