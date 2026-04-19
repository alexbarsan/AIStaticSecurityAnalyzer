# Feature 01 - Project-Aware Scan Engine And Correctness Hardening

## Goal

Replace directory-level `.cs` file scanning with a project-aware analysis pipeline that can load `.sln` or `.csproj` inputs, resolve references correctly, skip generated noise, and produce deterministic findings.

## Why This Is First

The current analyzer builds a compilation from raw files plus a minimal metadata reference set. That is acceptable for a demo, but it will not scale to realistic C# projects and it will weaken every rule added after this point.

## Current Pain Points

- scan scope is `Directory.GetFiles(..., "*.cs")`
- semantic references are incomplete
- generated code and output folders are not excluded centrally
- rule registration is hardcoded
- console output can drift from final scored findings

## Scope

- support scanning:
  - a directory
  - a `.csproj`
  - a `.sln`
- load real compilations using a project-aware Roslyn path
- centralize file exclusion rules
- make finding generation deterministic
- fix ordering issues in the CLI so scoring and filtering happen before presentation/export

## Implementation Rule

This feature, and the rest of the roadmap after it, should be developed tests first whenever the behavior can be exercised locally.

## Current Implementation Status

Completed in the first slice:

- scan input now accepts:
  - a directory
  - a `.csproj`
  - a `.sln`
- source-file selection is deterministic
- central exclusions now skip:
  - `bin`
  - `obj`
  - `.git`
  - common generated file suffixes such as `.g.cs` and `.Designer.cs`
- analyzer tests were added for `.csproj`, `.sln`, and exclusion behavior

Still remaining for later slices:

- real MSBuild workspace/project loading
- richer reference resolution for project/package dependencies
- explicit rule catalog/provider refactoring
- CLI output ordering so console printing always reflects post-AI/post-filter findings

## Suggested Design

- add a new abstraction in `Analyzer.Core` such as `ICompilationProvider`
- create a Roslyn/MSBuild-backed implementation in `Analyzer.Roslyn`
- return one or more analysis targets with:
  - project name
  - syntax trees
  - semantic models
  - root path
- move rule registration into a provider or catalog
- add scan exclusions for:
  - `bin`
  - `obj`
  - `.git`
  - generated files
  - output artifacts such as `analysis-report.json` and `analysis.sarif.json`

## Dependencies

- none, but this should be done before adding multiple new rules

## Acceptance Criteria

- analyzer can scan a `.csproj` directly
- analyzer can scan a `.sln` directly
- semantic model resolution works against real project references
- findings remain stable across repeated scans
- AI scoring, filtering, console printing, and exports all operate on the same final finding set

## Dissertation Value

This feature upgrades the project from a code-sample analyzer toward a real static-analysis pipeline, which strengthens the academic credibility of every later rule.
