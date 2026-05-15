# Academic Log - Feature 01 Slice 5

## Date

2026-04-22

## Scope

This slice completed Feature 01 by moving project file selection from raw `.csproj` XML parsing toward evaluated MSBuild state for project inputs.

## Test-First Evidence

The slice started with new regression tests that required evaluated build behavior:

- `Directory.Build.props` can disable default compile items for a project
- conditioned `ProjectReference` items respect evaluated MSBuild properties

These tests are meaningful because they are not satisfied by simple XML inspection alone.

## Implementation Summary

- kept the Roslyn-based rule engine and compilation flow
- updated project input resolution so `.csproj` scanning calls `dotnet msbuild -getItem:Compile` and `-getItem:ProjectReference`
- used evaluated `Compile` items as the source of truth for project scan scope
- kept recursive project-reference traversal with cycle protection
- retained the older XML-based resolver as a fallback path if MSBuild evaluation fails

## Validation

Validation covered:

- solution build
- full automated regression suite
- CLI smoke execution on a real `.csproj` input

## Academic Relevance

This change is important because it closes the gap between a path-based analyzer and a build-aware analyzer. Imported build configuration, conditional project references, and evaluated compile items now influence the analysis scope in a way that more closely matches real .NET project structure.

## Result

Feature 01 is treated as complete for the current dissertation roadmap baseline.

## Future Improvement

If later rules require deeper semantic fidelity, the next upgrade should focus on richer metadata/package reference resolution rather than on file-selection correctness, because project file selection is now substantially stronger than it was in the original prototype.
