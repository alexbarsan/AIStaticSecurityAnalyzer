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

Completed in the second slice:

- `.csproj` file discovery now respects basic project compile items:
  - `EnableDefaultCompileItems`
  - `Compile Include`
  - `Compile Remove`
- analyzer tests were added for:
  - explicit include-only projects
  - removal of files from the compile set

Completed in the third slice:

- the CLI post-analysis path now uses a consistent final pipeline:
  - enrich
  - AI score
  - confidence filter
  - console render
  - export
  - exit code
- invalid analysis paths now return a friendly console error instead of an unhandled stack trace
- analyzer tests were added for:
  - final pipeline ordering
  - user-friendly invalid-path messaging

Completed in the fourth slice:

- `.csproj` file discovery now walks basic `ProjectReference` graphs recursively
- referenced projects contribute source files to the same deterministic analysis set
- recursion is guarded so cyclic project references do not loop or duplicate findings
- analyzer tests were added for:
  - findings coming from referenced projects
  - compile-item behavior inside referenced projects
  - cycle-safe project-reference traversal

Completed in the fifth slice:

- `.csproj` inputs now use evaluated MSBuild item discovery through `dotnet msbuild`
- imported build state now affects scan scope, including:
  - `Directory.Build.props`
  - conditioned `ProjectReference` items
  - evaluated `Compile` items after imports and conditions
- the analyzer keeps the Roslyn rule engine, but project file selection is now based on evaluated build results instead of raw XML only
- analyzer tests were added for:
  - `Directory.Build.props` disabling default compile items
  - conditioned project references controlled by evaluated MSBuild properties

Feature 01 is considered complete for the current dissertation roadmap.

Possible future hardening beyond this feature:

- resolve package/framework metadata from evaluated build outputs instead of trusted platform assemblies only
- replace the current `dotnet msbuild` bridge with a dedicated in-process compilation provider if deeper build fidelity becomes necessary
- explicit rule catalog/provider refactoring

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
