# Codex Context — AI Static Security Analyzer

## Project identity

**Repository name:** `AIStaticSecurityAnalyzer`  
**Primary language:** C# / .NET  
**Primary IDE:** Visual Studio 2022  
**Author:** Alex Bârsan  
**Purpose:** Master’s dissertation project

## Dissertation context

**Recommended thesis direction from professor:**  
A static analyzer with AI that can be integrated into a CI/CD pipeline and analyze code for known vulnerability patterns, enriched with CVE information.

**Working dissertation theme/title (English):**  
**AI-Assisted Static Analysis Tool for Detecting Vulnerabilities in CI/CD Pipelines with CVE-Based Threat Intelligence Integration**

**Romanian version:**  
**Analiză statică asistată de inteligență artificială pentru detectarea vulnerabilităților software în pipeline-uri CI/CD utilizând surse CVE și clasificare automată**

## High-level goal

Build a modular, thesis-grade **SAST** tool in .NET that:

- scans C# source code using Roslyn
- detects security issues through rules
- maps findings to **CWE**
- enriches findings with **CVE / CVSS** from NVD
- uses **ML.NET** for confidence scoring / false-positive reduction
- outputs **JSON** and **SARIF**
- integrates with **GitHub Code Scanning**
- can act as a **CI/CD security gate** via exit codes

## Constraints / intended scope

This is an **MVP but academically defensible**, not a commercial-grade scanner.

### In scope
- C# / .NET source analysis
- Roslyn-based static analysis
- Semantic-model-based rules
- CWE/CVE/CVSS enrichment
- ML.NET confidence scoring
- JSON + SARIF reporting
- GitHub Actions / code scanning integration
- CI/CD fail-on-severity behavior

### Explicitly out of scope
- multi-language support
- full enterprise-grade dataflow engine
- training custom deep learning models
- perfect precision / recall
- zero false positives
- full CodeQL/SonarQube feature parity

---

# Environment and stack

## Main stack
- **.NET 8**
- **C#**
- **Visual Studio 2022**
- **Roslyn** (`Microsoft.CodeAnalysis.CSharp`)
- **SQLite** for local CVE storage
- **ML.NET** for binary classification / confidence scoring
- **SARIF 2.1.0**
- **GitHub Actions**
- **GitHub Code Scanning**

## Important packages used
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.Data.Sqlite`
- `Microsoft.ML`

## External sources
- **NVD API 2.0**
- **CWE**
- **CVSS**

## Important environment variable
- `NVD_API_KEY`

This must be obtained from NVD/NIST and passed as an environment variable, locally or in CI.

---

# Solution structure

The solution was intentionally designed with clean separation of concerns.

## Projects

- `Analyzer.CLI`  
  Entry point, orchestration, CLI arguments, exit codes, CI/CD behavior.

- `Analyzer.Core`  
  Domain models and interfaces. No Roslyn/AI/CVE logic should leak into this layer.

- `Analyzer.Roslyn`  
  Static analysis engine using Roslyn syntax + semantic model.

- `Analyzer.CVE`  
  NVD integration, SQLite storage, CVE/CVSS enrichment.

- `Analyzer.AI`  
  ML.NET training + scoring for false-positive reduction / prioritization.

- `Analyzer.Reporting`  
  JSON report writer, SARIF writer, CSV training export.

## Initial architecture goal
The CLI is the startup project and orchestrates all other modules.

---

# Day-by-day history

## Day 1 — Solution setup
Created the empty solution structure with these projects:

- `Analyzer.CLI` — Console App
- `Analyzer.Core` — Class Library
- `Analyzer.Roslyn` — Class Library
- `Analyzer.CVE` — Class Library
- `Analyzer.AI` — Class Library
- `Analyzer.Reporting` — Class Library

### Important setup decisions
- `Analyzer.CLI` is the **startup project**
- `Models` and `Interfaces` folders were added **only** in `Analyzer.Core`
- Temporary placeholder classes were renamed / later removed
- Build warning about “unable to find a project to restore” happened because build was run from the wrong folder

## Day 2 — Core domain model
Implemented the core domain layer in `Analyzer.Core`.

### Main models/interfaces
- `Severity`
- `Vulnerability`
- `Finding`
- `AnalysisResult` (optional but useful)
- `IRule`
- `ICodeAnalyzer`

### Design intent
- `Finding` represents actual detections in files
- `Vulnerability` carries metadata such as CWE, description, recommendation
- `IRule` is the rule abstraction
- `ICodeAnalyzer` is the orchestration abstraction

## Day 3 — First real rule with Roslyn
Implemented first real detection in `Analyzer.Roslyn`:

### Rule
- **Weak Hashing**
- CWE: `CWE-327`
- Detects insecure hashing usage like **MD5** / **SHA1**

### Components
- `RoslynCodeAnalyzer`
- `WeakHashingRule`

### Initial approach
- AST-based traversal
- Parse C# files
- Detect invocation expressions containing MD5/SHA1
- Produce `Finding`
- CLI prints findings and returns exit code

## Day 4 — Hardcoded secrets rule
Added second real security rule:

### Rule
- **Hardcoded Secret**
- CWE: `CWE-798`
- Detects secrets embedded in source code

### Initial detection strategy
- look for variable names like:
  - password
  - pwd
  - secret
  - token
  - apiKey
  - api_key
- inspect string literal initializers
- simple length-based heuristic

## Day 5 — Semantic model upgrade
This was an important jump in technical quality.

### What changed
Rules were upgraded from text/syntax-only style to use the **Roslyn semantic model**.

### Key changes
- `IRule` changed from simple string-based analysis to semantic analysis using:
  - `SyntaxNode root`
  - `SemanticModel semanticModel`
  - `filePath`
- `RoslynCodeAnalyzer` now builds a `CSharpCompilation`
- `GetSemanticModel(tree)` is used for each syntax tree

### Why this matters
This moved the project from “smart pattern matching” toward **real static analysis**.

### Rules upgraded semantically
- `WeakHashingRule`
  - now resolves symbols and containing types
  - can detect aliased or semantically resolved crypto APIs
- `HardcodedSecretRule`
  - started using declared symbols
  - initially focused on locals, then expanded

## Day 6 — Reporting layer
Added reporting and machine-readable output.

### Reporting models / classes
- `ReportSummary`
- `AnalysisReport`
- `JsonReportWriter`

### CLI capabilities
- `--json`
- Writes `analysis-report.json`

### CI/CD behavior
- exit code based on max severity

## Day 6.1 — Hardcoded secrets robustness
Improved `HardcodedSecretRule`.

### Important improvements
- supported more symbol kinds:
  - `ILocalSymbol`
  - `IFieldSymbol`
- added recommended null checks around `GetDeclaredSymbol(...)`
- fixed detection gap where `apiKey` was not being detected

### Important engineering note
A null check around `GetDeclaredSymbol` was explicitly considered important and should remain.

## Day 6.2 — More secret-detection coverage
Expanded `HardcodedSecretRule` to cover more real cases.

### Added support for
- local variable initializers
- field initializers
- const locals / const fields
- property initializers
- assignment expressions:
  - `token = "..."`
  - `this.apiKey = "..."`
  - `_password = "..."`
- simple secret heuristics based on value shape

### Shapes / heuristics considered
- JWT-like strings
- base64-ish strings
- long mixed alphanumeric strings

## Day 6.3 — Noise reduction + pipeline friendliness
Two upgrades happened here.

### 1) False-positive reduction in secret rule
Ignored obvious non-secrets such as:
- URLs (`http://`, `https://`, etc.)
- localhost / loopback values
- placeholder strings like:
  - `test`
  - `demo`
  - `example`
  - `changeme`
  - `your_api_key`
- GUID-like values
- strings with whitespace
- very short values

### 2) CLI fail threshold
Added:
- `--fail-on <info|low|medium|high|critical>`

This made pipeline failure configurable.

---

## Day 7 — CVE/CVSS enrichment
Added NVD + SQLite integration.

### Main intent
Map findings from **CWE → best matching CVE** and include CVSS info.

### Main components
- `DbInitializer`
- `CveRepository`
- `NvdClient`
- `NvdImporter`
- `FindingEnricher`

### Storage
SQLite database:
- `cves.db`

### NVD sync
CLI mode:
- `sync-nvd --days 7`

### Important notes
- `NVD_API_KEY` is optional but recommended
- without it, rate limiting is much worse
- enrichment is **best-effort**, not guaranteed exact mapping

### Important model update
`Finding` was extended with optional enrichment fields:
- `CveId`
- `CvssBaseScore`
- `CvssSeverity`

## Day 8 — AI scoring with ML.NET
Added AI-assisted confidence scoring.

### Philosophy
AI is used for:
- prioritization
- false-positive reduction
- confidence scoring

Not for “magic vulnerability discovery”.

### Components
- `AiInput`
- `AiOutput`
- `AiScorer`
- `TrainModel`
- training CSV

### Main features used for ML
- RuleId
- CweId
- SnippetLength
- HasJwtShape
- HasBase64Shape
- HasUrlShape
- HasPlaceholderValue

### Training
CLI mode:
- `train-ai`

### Inference
CLI flags:
- `--ai`
- later `--min-confidence <0..1>`

### Important ML.NET issue encountered
There was a `Label column does not exist` error.
It was fixed by:
- using `[LoadColumn(...)]` on `AiInput`
- explicitly setting label/feature columns in the trainer

### Important ML.NET issue encountered
AUC crashed on tiny dataset:
- `AUC is not defined when there is no positive class in the data`
This happened because the test split had only one class.

### Fix
Training code was made more robust:
- larger split fraction for tiny dataset
- print class distribution
- skip AUC or print `N/A` when only one class is present in the test set

## Day 8.1 — AI threshold + evaluation
Added:
- `--min-confidence <0..1>` to filter findings below a confidence threshold
- training metrics printing:
  - Accuracy
  - F1
  - AUC (if valid)

This allowed AI to affect CI/CD more directly.

### Additional capability
Added CSV export for future labeling / retraining.

## Training export
Added:
- `CsvTrainingExporter`
- CLI flag:
  - `--export-training <file.csv>`

### Important path issue encountered
Exported CSV ended up in a different location than the canonical training file.

### Recommended resolution
Treat `Analyzer.AI/Training/training-data.csv` as the canonical dataset, or resolve relative export paths deterministically.

---

## Day 9 — SARIF output
Added SARIF 2.1.0 export.

### Main components
- `SarifModels`
- `SarifReportWriter`

### CLI support
- `--sarif`
- optional explicit file path
- default file name such as `analysis.sarif.json`

### SARIF content includes
- tool metadata
- rules
- results
- locations
- properties such as:
  - cweId
  - confidence
  - cveId
  - cvssBaseScore
  - cvssSeverity

### Important SARIF issue encountered
GitHub rejected SARIF because:
- `informationUri` existed but was not a valid string

### Fix
Make `informationUri` nullable and **omit it when null**, using:
- `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`

---

## Day 9.1 — GitHub Actions + GitHub Code Scanning
Integrated the analyzer into GitHub Actions.

### Workflow file
- `.github/workflows/code-scanning.yaml`

### Important mistakes encountered
- folder was initially misspelled (`worflows`)
- YAML formatting/indentation/newline issues
- branch needed to use `master`, not `main`
- conditional use of `secrets` in `if:` caused a parsing problem

### Resolutions
- correct folder: `.github/workflows`
- proper multiline YAML
- trigger on `master`
- for optional NVD sync, use shell logic instead of `if: ${{ secrets... }}`

### Final behavior
Workflow:
- restores/builds solution
- optionally syncs NVD
- runs analyzer to generate SARIF
- uploads SARIF to GitHub Code Scanning
- enforces security gate afterward

### Important state reached
At one point the workflow successfully:
- generated SARIF
- uploaded SARIF
- then failed at the security gate because analyzer returned exit code `2`

This is expected because:
- findings of High/Critical severity were present
- the gate was intentionally configured to fail

### Findings seen in CI logs
The workflow log showed actual findings such as:
- weak hashing
- hardcoded secret
- with confidence and CVE/CVSS enrichment

This means the analyzer, SARIF generation, and upload path were working.

---

# Current known features

## CLI modes / options implemented or discussed
- scan a path
- `--json`
- `--sarif [file]`
- `--ai`
- `--min-confidence <0..1>`
- `--fail-on <severity>`
- `--export-training <csv>`
- `sync-nvd --days <n>`
- `train-ai`

## Core detection rules currently implemented
1. **Weak Hashing**
   - detects MD5 / SHA1 style usage
   - semantic-model aware
   - CWE-327
   - severity High

2. **Hardcoded Secrets**
   - semantic-model aware
   - supports locals / fields / properties / assignments
   - heuristics for token-like values
   - false-positive filtering for obvious non-secrets
   - CWE-798
   - severity Critical

---

# Important technical design choices

## Why Roslyn
Roslyn was chosen because:
- it is native to C#
- provides syntax trees and semantic models
- integrates naturally with .NET tooling
- is academically justifiable for static analysis

## Why SQLite
SQLite was chosen because:
- simple local persistence
- no SQL Server setup overhead
- easy for PoC / dissertation
- sufficient for local CVE cache

## Why ML.NET
ML.NET was chosen because:
- native .NET integration
- easy to explain in thesis
- enough for binary classification / confidence scoring
- avoids infrastructure overhead of external ML services

## Why SARIF
SARIF was chosen because:
- industry standard
- GitHub Code Scanning supports it
- enables CI/CD integration and professional demos

---

# Known issues / caveats / lessons learned

## General
- CI can fail intentionally because the analyzer is configured as a security gate
- this is expected, not necessarily a bug

## HardcodedSecretRule
- should keep robust null checks around symbol resolution
- should continue to avoid obvious placeholder values
- heuristic tuning is still possible

## ML
- dataset is still very small
- metrics are not meaningful unless more labeled data is collected
- AUC may be invalid on tiny splits
- `training-data.csv` management should be kept consistent

## CVE enrichment
- CWE → CVE mapping is best-effort
- not every finding maps perfectly
- stored CVE chosen is usually “best match” based on score / recency

## SARIF / GitHub
- paths should ideally resolve to repo-relative files
- GitHub Code Scanning may take a short time to display results
- workflow success/failure should be interpreted separately from SARIF upload success

---

# Likely next step (the next major engineering day)

The next recommended feature was:

## Day 10 — SQL Injection with basic taint/dataflow
Planned rule:
- **SQL Injection**
- CWE-89

Planned technical depth:
- define taint sources
- define sinks
- track simple propagation
- detect non-parameterized SQL use

This was explicitly recommended as the next meaningful technical milestone because it would move the analyzer beyond mostly pattern-based rules into **basic taint/dataflow analysis**.

Alternative next steps that were considered:
- enhanced SARIF taxonomy mapping
- Dockerization
- cross-file analysis
- Jenkins integration

But the strongest next technical feature was **SQL Injection / taint tracking**.

---

# What Codex should know before continuing

## Important project goals
Codex should preserve these priorities:
1. keep architecture modular
2. avoid collapsing everything into CLI
3. maintain thesis-grade clarity and justification
4. prefer explainable approaches over “AI magic”
5. preserve CI/CD compatibility
6. keep SARIF valid
7. preserve semantic-model-based analysis

## Coding style / engineering preferences established in this thread
- robust null checks are important
- favor explicitness over cleverness
- keep rules explainable for academic defense
- prefer incremental extensions over overengineering
- AI is an enhancer, not the primary detection engine
- avoid introducing unnecessary dependencies unless justified

## Things that should probably be cleaned up/refactored
- normalize nullable settings across projects (`<Nullable>enable</Nullable>`)
- ensure canonical training dataset path is consistent
- confirm SARIF path mapping is repo-relative and stable
- possibly improve README / docs / diagrams
- possibly add tests if not already present

---

# Suggested README / presentation framing

The project should be framed as:

> A modular AI-assisted static security analyzer for C#/.NET that combines rule-based analysis, CWE/CVE/CVSS enrichment, ML.NET-based confidence scoring, SARIF reporting, and CI/CD security gating.

This is the correct positioning for the dissertation and demos.

---

# Commands and examples

## Local scan
```bash
dotnet run --project Analyzer.CLI -- ./ --json
```

## Scan with AI
```bash
dotnet run --project Analyzer.CLI -- ./ --ai --min-confidence 0.7
```

## SARIF export
```bash
dotnet run --project Analyzer.CLI -- ./ --sarif analysis.sarif.json
```

## Fail on severity
```bash
dotnet run --project Analyzer.CLI -- ./ --fail-on high
```

## Train AI
```bash
dotnet run --project Analyzer.CLI -- train-ai
```

## Export training examples
```bash
dotnet run --project Analyzer.CLI -- ./ --ai --export-training training-data.csv
```

## Sync NVD
```bash
dotnet run --project Analyzer.CLI -- sync-nvd --days 7
```

---

# Final summary for Codex

This project is **already beyond MVP** and is in the stage where the next value comes from:

- improving technical depth
- adding a dataflow-aware vulnerability rule
- refining path/reporting/CI behavior
- stabilizing and polishing

The most important unfinished feature is:
- **SQL Injection detection with basic taint/dataflow analysis**

Codex should continue from the current solution state and preserve all the architectural and thesis-oriented decisions above.
