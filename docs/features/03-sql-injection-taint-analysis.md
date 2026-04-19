# Feature 03 - SQL Injection Detection With Basic Taint Analysis

## Goal

Implement the first intra-procedural taint/dataflow rule: SQL injection detection (`CWE-89`).

## Why This Is The Highest-Value Detection Rule

This is the first feature that materially changes the analyzer from pattern matching into explainable dataflow-oriented analysis. It is also easy to defend academically because the source, propagation, sink, and sanitizer concepts are standard static-analysis concepts.

## Scope

- define taint sources
- define propagation steps
- define SQL sinks
- define sanitizer/mitigation patterns
- report only when tainted data reaches a sink without safe parameterization

## Candidate Sources

- controller parameters
- method parameters in web/service layers
- values from `Request.Query`, `Request.Form`, `RouteData`, or similar web inputs
- environment or config values only if they later prove useful for the codebase

## Candidate Sinks

- `SqlCommand.CommandText`
- `new SqlCommand(<query>, ...)`
- `ExecuteSqlRaw`
- string-built queries passed to ADO.NET or EF raw SQL methods

## Safe Patterns To Recognize

- parameterized `SqlParameter` usage
- interpolated SQL APIs that are explicitly safe
- prepared statements when clearly modeled

## Technical Boundary

Keep version 1 intra-procedural:

- local variable assignments
- simple concatenation
- interpolation
- direct method-scope propagation

Do not attempt inter-procedural or whole-program taint in v1.

## Dependencies

- strongly benefits from Feature 01
- should be protected by Feature 02 tests

## Acceptance Criteria

- detects string concatenation of tainted input into SQL sinks
- detects interpolated tainted SQL strings
- avoids flagging clearly parameterized queries
- emits findings with CWE `CWE-89`

## Dissertation Value

This is the strongest next milestone because it demonstrates real static-analysis depth rather than only syntax heuristics.
