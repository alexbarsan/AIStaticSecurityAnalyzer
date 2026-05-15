# Academic Log - Feature 01 Slice 2

## Date

2026-04-22

## Feature

Feature 01 - Project-Aware Scan Engine And Correctness Hardening

## Slice Goal

Extend the project-aware scan engine so `.csproj` analysis does not only scan the project directory blindly, but also respects basic compile-item declarations from the project file itself.

## Context

At the start of this slice, the analyzer already supported:

- directory input
- `.csproj` input
- `.sln` input
- deterministic file ordering
- central exclusion of `bin`, `obj`, `.git`, and common generated files

However, `.csproj` handling still behaved like a directory scan. This meant the analyzer ignored project-level compile settings such as:

- `Compile Remove`
- `Compile Include`
- `EnableDefaultCompileItems=false`

That gap reduced the academic credibility of the "project-aware" claim, because the scanner was not yet honoring basic project metadata.

## Test-First Method

This slice followed the project rule that feature work should begin with automated tests whenever the behavior can be exercised locally.

Two failing tests were added first:

1. `csproj respects Compile Remove items`
2. `csproj explicit Compile Include works when default compile items are disabled`

The failing baseline showed that the analyzer still returned findings from files that the project file had excluded from compilation.

## Implementation Summary

The main implementation was added in:

- `Analyzer.Roslyn/AnalysisInputResolver.cs`

### Technical changes

- loaded the `.csproj` file as XML using `XDocument`
- detected whether default compile items are enabled
- collected `Compile Include` item specifications
- collected `Compile Remove` item specifications
- built the final source-file set by:
  - starting from default directory-based compile items when enabled
  - adding explicitly included files
  - removing explicitly removed files
- preserved deterministic ordering of the final source-file list

### Supported compile-item behavior in this slice

- `EnableDefaultCompileItems=false`
- exact-path `Compile Include`
- exact-path `Compile Remove`
- limited wildcard support via file enumeration

## Validation

Validation completed with:

- targeted test run for the new compile-item tests
- full build/test run after the implementation

Observed result:

- both new tests passed after implementation
- the previously existing test suite continued to pass

## Academic Value

This slice improves the methodological strength of the dissertation in two ways:

1. It makes the analyzer more faithful to how C# projects actually define their source set.
2. It demonstrates an incremental, test-driven approach to evolving static-analysis infrastructure.

This is important because dissertation evaluation often considers not only the final functionality, but also whether the engineering process is rigorous and justified.

## Limitations

This slice still does not provide full MSBuild workspace semantics.

Important remaining limitations:

- no real project evaluation through Roslyn/MSBuild workspace
- no package/project reference resolution from evaluated MSBuild state
- compile-item condition handling is still limited
- wildcard handling is basic rather than fully MSBuild-compatible

## Next Recommended Slice

Continue Feature 01 with one of these two directions:

1. friendly CLI error handling for invalid analysis paths
2. deeper project loading through Roslyn/MSBuild workspace

The second option is architecturally stronger, but the first one is smaller and easier to validate immediately.
