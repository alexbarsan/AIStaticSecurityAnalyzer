# Feature 06 - Insecure Deserialization Detection

## Goal

Detect unsafe deserialization patterns (`CWE-502`) in .NET code.

## Why This Rule Matters

It broadens the analyzer beyond taint-only cases and targets a high-impact vulnerability family that is still explainable through API misuse detection.

## Scope

- detect unsafe usage of known risky serializers or settings
- focus on recognizable high-risk APIs and configurations
- optionally score findings higher when untrusted data is clearly involved

## Candidate APIs And Patterns

- `BinaryFormatter`
- `NetDataContractSerializer`
- risky `Json.NET` settings such as `TypeNameHandling` when used unsafely
- XML serializers only where the risk is established by code patterns

## Suggested Design

- start with precise API-based detections
- add light contextual checks for whether the payload source is external
- attach strong remediation guidance in finding metadata

## Non-Goals For v1

- full serializer safety modeling
- framework-wide deserialization source tracing

## Dependencies

- Feature 02 tests
- Feature 09 reporting improvements will make rule guidance clearer, but are not required

## Acceptance Criteria

- detects explicit use of risky serializer APIs
- maps findings to `CWE-502`
- provides a concrete remediation message

## Dissertation Value

This feature adds rule diversity and shows the analyzer is not limited to only string concatenation cases.
