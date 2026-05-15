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

### 1. Scan a path

```powershell
dotnet run --project Analyzer.CLI -- . 
```

This scans the target path, prints findings to the console, enriches them from `cves.db`, and returns an exit code based on the highest severity found.

The console output now reflects the same final finding set that is used for export and exit-code evaluation after AI scoring and confidence filtering.

Supported scan inputs:

- a directory
- a `.csproj`
- a `.sln`

Current `.csproj` awareness includes:

- evaluated `Compile` items from `dotnet msbuild`
- imported build settings such as `Directory.Build.props`
- conditioned `ProjectReference` items after MSBuild evaluation
- recursive `ProjectReference` traversal with cycle protection

Examples:

```powershell
dotnet run --project Analyzer.CLI -- .\MyProject\MyProject.csproj
dotnet run --project Analyzer.CLI -- .\MySolution.sln --json
```

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

- `Analyzer.AI/Training/training-labeled.csv`

Output:

- `ai-model.zip`

### 8. Export findings as ML training candidates

```powershell
dotnet run --project Analyzer.CLI -- . --export-training training-candidates.csv
```

Current behavior:

- relative exports are written under `Analyzer.AI/Training/`
- the canonical default file is `training-candidates.csv`
- exported rows are unlabeled candidates for later manual labeling
- export-only runs skip the security gate unless `--fail-on` is explicitly provided

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
- `Analyzer.AI/Training/training-labeled.csv`
  - canonical labeled training dataset
- `Analyzer.AI/Training/training-candidates.csv`
  - exported unlabeled candidate dataset

## Known Limitations

- the analyzer now accepts directories, `.csproj`, and `.sln`, and project inputs use evaluated MSBuild compile/project items to determine scan scope
- metadata reference resolution is still lighter than a full design-time build; the current rules work well for the present dissertation scope, but deeper future semantic rules may need richer package/framework reference loading
- invalid scan paths now fail with a friendly error message, but full MSBuild workspace loading is still not implemented
- only two rules are implemented
- only a small automated regression runner exists today; overall coverage is still limited
- current AI workflow is useful for experimentation, but not yet mature enough for rigorous evaluation
- CVE enrichment is best-effort and not a direct proof that a detected pattern maps to one exact CVE

## Recommended Next Reading

- [README.md](../README.md)
- [docs/CODEX_CONTEXT_V2.md](CODEX_CONTEXT_V2.md)
- [docs/features/03-sql-injection-taint-analysis.md](features/03-sql-injection-taint-analysis.md)
