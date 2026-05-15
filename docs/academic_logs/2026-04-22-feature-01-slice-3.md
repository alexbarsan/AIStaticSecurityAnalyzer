# Academic Log - Feature 01 Slice 3

## Date

2026-04-22

## Feature

Feature 01 - Project-Aware Scan Engine And Correctness Hardening

## Slice Goal

Improve the CLI analysis pipeline so that console output, filtering, exports, and exit-code calculation all operate on the same final finding set, and replace raw invalid-path crashes with user-friendly errors.

## Context

After the previous Feature 01 slices, the analyzer had already become path-aware and basic `.csproj`-aware. However, one correctness issue remained in the CLI:

- findings were printed before AI scoring and confidence filtering

This meant the console output could diverge from:

- JSON export
- SARIF export
- training export
- final gate result

In addition, invalid analysis paths still surfaced as unhandled exceptions during real CLI runs.

## Test-First Method

This slice followed the project rule that feature work should start with tests whenever the behavior can be exercised locally.

Two new tests were added first:

1. `final pipeline applies scoring before filtering and rendering`
2. `invalid analysis path returns a friendly console message`

These tests targeted pure behavior rather than shell parsing, so they remained fast and deterministic.

## Implementation Summary

The implementation introduced small reusable core components:

- `Analyzer.Core/Pipeline/ScanPipelineProcessor.cs`
- `Analyzer.Core/Pipeline/FindingConsoleFormatter.cs`
- `Analyzer.Core/Pipeline/ScanPipelineResult.cs`
- `Analyzer.Core/Execution/ScanErrorFormatter.cs`

### Technical changes

- extracted final finding processing into a dedicated pipeline helper
- ensured the processing order is:
  1. AI scoring
  2. confidence filtering
  3. console rendering
  4. export
  5. exit-code evaluation
- moved console-line formatting into a reusable formatter
- added friendly formatting for invalid-path and unsupported-input errors
- updated `Analyzer.CLI/Program.cs` to use the new helpers

## Validation

Validation completed with:

- full solution build
- full regression suite
- CLI smoke test with a missing analysis path

Observed result:

- the test suite passed
- the CLI now returns exit code `1` for a missing path
- the missing path is presented as a short user-facing error instead of a stack trace

## Academic Value

This slice improves the dissertation quality in two ways:

1. It increases behavioral consistency between what the tool prints and what it actually exports or gates on.
2. It shows attention to tool usability and reproducibility, which matters in academic demonstrations and evaluations.

A static analysis tool should not only detect issues; it should also present results consistently enough to be trustworthy during experiments and demos.

## Limitations

This slice does not complete Feature 01 fully.

Important remaining limitations:

- no MSBuild workspace loading
- no evaluated project/package reference graph
- no richer project-system semantics beyond basic compile items
- rule registration is still hardcoded

## Next Recommended Slice

The next major slice should target true MSBuild/Roslyn workspace integration, because that is now the main remaining blocker for calling Feature 01 complete.
