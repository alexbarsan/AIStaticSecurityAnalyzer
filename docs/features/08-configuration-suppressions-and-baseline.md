# Feature 08 - Configuration, Suppressions, And Baseline Mode

## Goal

Add a real analyzer configuration system so the tool can be tuned, adopted incrementally, and used more credibly in CI/CD.

## Why It Is Needed

Without configuration and suppression support, the analyzer will become harder to use as rule count increases. This is especially important for dissertation demos because reviewers will expect some false-positive management strategy.

## Scope

- add a configuration file, for example `analyzer.json`
- support rule enable/disable
- support severity overrides
- support path exclusions
- support suppression by:
  - rule id
  - file path
  - stable finding fingerprint
- add baseline mode for "only fail on new findings"

## Suggested Design

- create config models in `Analyzer.Core`
- load config in `Analyzer.CLI`
- apply config before output and gating
- keep suppression decisions visible in reports for auditability

## Dependencies

- Feature 09 benefits from stable fingerprints

## Acceptance Criteria

- users can disable a rule without code changes
- users can exclude paths from scans
- users can suppress known findings cleanly
- baseline mode can compare current findings with a stored baseline file

## Dissertation Value

This feature makes the analyzer look like a serious tool rather than a one-shot scanner, while still remaining manageable in scope.
