# Feature 05 - Path Traversal And Unsafe File Access Rule

## Goal

Detect path traversal risks (`CWE-22`) where untrusted input controls file-system paths used for reads or writes.

## Why It Belongs In The First Rule Family

It is another strong taint-analysis rule that fits naturally after SQL injection and command injection. Together, these three rules create a coherent "untrusted input to dangerous sink" thesis chapter.

## Scope

- detect tainted input flowing into:
  - `File.ReadAllText`
  - `File.WriteAllText`
  - `File.Open*`
  - `Directory.*`
  - `Path.Combine` chains that end in file access
- model common path traversal indicators:
  - `../`
  - rooted paths
  - user-controlled filename joins

## Safe Patterns To Recognize

- explicit root confinement checks
- allowlisted filenames or extensions
- canonicalization followed by prefix validation when clearly visible in the same method

## Implementation Approach

- extend the taint engine with file-system sinks
- track simple path-building operations
- keep the analysis intra-procedural and explainable

## Dependencies

- Feature 03 taint foundation
- Feature 02 tests for positive and negative fixtures

## Acceptance Criteria

- flags user-controlled paths reaching file-system APIs
- avoids flagging constant paths
- maps findings to `CWE-22`

## Dissertation Value

This adds a third meaningful sink family and shows the analyzer can generalize beyond SQL-only examples.
