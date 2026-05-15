# Feature 02 - Automated Test Harness And Regression Fixtures

## Goal

Create a real automated test suite for rules, CLI behavior, reporting, enrichment, and future taint-analysis logic.

## Why It Is Necessary

The repository currently has no actual test project. Adding more rules without a regression harness will make the dissertation demo fragile and will slow down later development.

## Scope

- add a dedicated test project
- introduce fixture-based rule tests
- add reporter snapshot tests for JSON and SARIF
- add CLI integration tests for exit codes and command modes
- add taint-analysis fixtures before SQL injection is implemented

## Current Implementation Status

Completed in the first slice:

- fixture-backed regression tests were added for the two implemented rules:
  - weak hashing
  - hardcoded secrets
- positive and negative samples now live under `Analyzer.Tests/Fixtures/Rules/`
- the test harness loads fixture files from disk instead of relying only on inline synthetic snippets
- the fixture tree is excluded from compilation so it behaves as test data, not production code

Still remaining:

- move the runner toward a conventional unit/integration layout if desired
- add reporting snapshots for JSON and SARIF
- add CLI integration coverage for exit codes and command modes
- add enrichment and AI dataset regression coverage

## Suggested Structure

- `Analyzer.Tests`
  - `Rules/`
  - `Cli/`
  - `Reporting/`
  - `Fixtures/`

Each rule test should include:

- a positive sample
- a negative sample
- expected location
- expected CWE and severity

## Suggested Test Types

- unit tests for rule logic
- integration tests for CLI flows
- snapshot tests for JSON and SARIF outputs
- enrichment tests using a temporary SQLite database
- training/export tests for AI dataset hygiene

## Dependencies

- Feature 01 improves test realism, but basic tests can start immediately

## Acceptance Criteria

- every implemented rule has positive and negative fixtures
- SARIF output is validated by tests
- fail-threshold behavior is tested
- new rules cannot be merged without tests

## Dissertation Value

Tests convert the project from a sequence of ad-hoc experiments into a repeatable research artifact.
