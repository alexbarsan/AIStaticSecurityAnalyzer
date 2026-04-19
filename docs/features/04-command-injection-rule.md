# Feature 04 - Command Injection / Unsafe Process Execution Rule

## Goal

Detect command injection risks (`CWE-78`) where untrusted input reaches OS command execution APIs.

## Why It Should Follow SQL Injection

It can reuse the taint concepts from Feature 03 with a different sink set. That makes it a high-value follow-up rule with good engineering leverage.

## Scope

- detect tainted input reaching:
  - `Process.Start`
  - `ProcessStartInfo.Arguments`
  - shell invocation patterns such as `cmd /c` or `powershell -Command`
- distinguish between command and argument construction where possible
- recognize simple sanitization or safe allowlist patterns only when clearly modeled

## Implementation Approach

- reuse the same taint source model from SQL injection where possible
- add command-execution sinks
- model string concatenation and interpolation
- report a finding when tainted data becomes part of the final command line

## Non-Goals For v1

- full shell grammar parsing
- inter-procedural sanitizer modeling
- platform-specific execution semantics beyond common .NET usage

## Dependencies

- Feature 03 should establish the taint-analysis foundation first

## Acceptance Criteria

- flags tainted input passed into command execution APIs
- does not flag static command strings with no external input
- maps findings to `CWE-78`

## Dissertation Value

This feature proves the taint engine is reusable across vulnerability classes, which is stronger than building isolated one-off heuristics.
