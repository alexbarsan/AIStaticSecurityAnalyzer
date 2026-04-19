# Codex Context - AI Static Security Analyzer v2

## Purpose

This file is the updated working context for future development sessions. It is based on the current repository contents, not only on the earlier planning notes.

## Project Identity

- Repository: `AIStaticSecurityAnalyzer`
- Solution: `AIStaticSecurityAnalyzer.sln`
- Main language: C#
- Current target frameworks in the repository: `net9.0`
- Primary style: modular dissertation project, not production-grade SAST

## Dissertation Positioning

The project should be presented as:

> A modular AI-assisted static security analyzer for C#/.NET that combines Roslyn-based rule analysis, CWE/CVE/CVSS enrichment, ML.NET confidence scoring, SARIF reporting, and CI/CD security gating.

That framing is academically defensible and still consistent with the current code.

## Current Modules

- `Analyzer.CLI`
  - entry point
  - CLI modes: scan, `sync-nvd`, `train-ai`
  - JSON export, SARIF export, fail threshold
- `Analyzer.Core`
  - `Finding`, `Vulnerability`, `Severity`
  - `ICodeAnalyzer`, `IRule`
- `Analyzer.Roslyn`
  - `RoslynCodeAnalyzer`
  - `WeakHashingRule`
  - `HardCodedSecretRule`
- `Analyzer.CVE`
  - `DbInitializer`
  - `CveRepository`
  - `NvdClient`
  - `NvdImporter`
  - `FindingEnricher`
- `Analyzer.AI`
  - `AiInput`, `AiOutput`
  - `AiScorer`
  - `TrainModel`
- `Analyzer.Reporting`
  - `JsonReportWriter`
  - `SarifReportWriter`
  - `CsvTrainingExporter`

## Confirmed Implemented Features

### Static analysis

- weak hashing detection
  - currently based on Roslyn semantic model
  - targets MD5 and SHA1 usage
  - mapped to `CWE-327`
- hardcoded secret detection
  - covers variable declarations, property initializers, and assignments
  - mapped to `CWE-798`

### Reporting

- JSON report generation
- SARIF 2.1.0 export
- CI/CD gate via exit code and `--fail-on`

### CVE enrichment

- SQLite cache in `cves.db`
- NVD sync using modified-date window
- best-effort enrichment by matching finding `CWE -> CVE`

### AI

- ML.NET model training from CSV
- ML.NET model loading and inference
- optional filtering with `--min-confidence`

### CI/CD

- GitHub Actions workflow uploads SARIF to GitHub Code Scanning

## Verified Gaps and Reality Checks

These points are important because the earlier context file is ahead of the code in a few places.

### What is true now

- The solution currently targets `net9.0`, not `net8.0`.
- There is a small dedicated automated test project, but coverage is still narrow.
- The scan engine is not project-aware; it uses `Directory.GetFiles(..., "*.cs")`.
- Semantic references are minimal, which will limit analysis on larger real projects.
- Rule registration is hardcoded in `RoslynCodeAnalyzer`.
- CLI parsing is manual and concentrated in `Program.cs`.

### Code-quality issues worth remembering

- `HardCodedSecretRule` still needs normalization and stronger false-positive handling.
- The secret rule metadata text is partly inaccurate; it says secrets are stored "in the environment variables" even though it detects hardcoded values in source code.
- AI scoring happens after findings are printed in `Program.cs`, so console output can drift from final scored results.
- CSV export currently writes unlabeled `-1` rows, which is useful for manual labeling but should not be treated as training-ready data.
- There are naming inconsistencies such as `AnalysesResult` and `Recommandation`.

## Architecture Guidance For Future Changes

When continuing implementation, preserve these principles:

1. Keep the modular project separation.
2. Do not move business logic into `Analyzer.CLI`.
3. Prefer explainable static-analysis logic over opaque "AI magic".
4. Add dataflow/taint analysis incrementally and keep it intra-procedural first.
5. Keep outputs machine-readable and CI-friendly.
6. Treat tests and rule fixtures as mandatory once the next rule family starts.

## Recommended Next 10 Features

The next backlog should be implemented in this order:

1. project-aware scan engine and correctness hardening
2. automated test harness and regression fixtures
3. SQL injection detection with basic taint analysis
4. command injection detection
5. path traversal detection
6. insecure deserialization detection
7. XXE and unsafe XML parser detection
8. configuration, suppressions, and baseline mode
9. reporting v2 with fingerprints and SARIF taxonomy improvements
10. AI dataset and confidence pipeline v2

Detailed specs are in `docs/features/`.

## Suggested Technical Changes Before Or During Feature Work

- Decide whether to stay on `.NET 9` or move to `.NET 8 LTS`, then align all docs.
- Introduce a real test project before adding several new rules.
- Expand the current test runner into broader rule, CLI, and reporting coverage before adding several new rules.
- Move toward project-aware loading, ideally through Roslyn/MSBuild workspace support.
- Add deterministic scan exclusions for `bin`, `obj`, `.git`, generated code, and report outputs.
- Normalize model/property naming to remove avoidable dissertation noise.

## Working Assumptions For Codex

- The repository is a dissertation project first, not a product.
- The user wants implementation help, not only theory.
- Roadmap work should optimize for defendability, demo quality, and incremental delivery.
- If time is limited, prioritize foundational quality plus one strong taint-based rule over a large number of weak pattern rules.
