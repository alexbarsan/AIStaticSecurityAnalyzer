# Academic Log - Feature 02 Slice 1

## Date

2026-05-15

## Scope

This slice started Feature 02 by introducing fixture-backed regression tests for the two implemented Roslyn rules:

- weak hashing
- hardcoded secrets

## Test-First Evidence

The change began with new failing tests that expected on-disk fixture inputs rather than inline synthetic snippets.

## Implementation Summary

- added rule fixtures under `Analyzer.Tests/Fixtures/Rules/`
- updated the test harness to load fixture files from disk
- excluded the fixture tree from compilation so the files are treated as test data only
- kept the existing console test runner for now, because the goal of this slice was fixture coverage, not a test framework migration

## Validation

Validated with:

- solution build
- full test runner execution

## Academic Relevance

Fixture-backed tests are important for dissertation quality because they make the current detection logic reproducible and easier to extend. They also create a pattern that later rules can follow when SQL injection and other taint-based detectors are added.

## Result

Feature 02 is now underway, with the first regression-fixture slice in place.
