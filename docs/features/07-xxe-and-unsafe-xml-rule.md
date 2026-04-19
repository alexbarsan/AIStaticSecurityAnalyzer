# Feature 07 - XXE And Unsafe XML Parser Configuration Rule

## Goal

Detect XML External Entity and unsafe XML parser configuration issues (`CWE-611`).

## Why It Is A Good Dissertation Rule

It is a classic secure-coding issue with clear, explainable API patterns and safe alternatives. It also complements the deserialization and taint-analysis work well.

## Scope

- inspect creation and configuration of:
  - `XmlDocument`
  - `XmlReaderSettings`
  - `XmlTextReader`
  - related XML parsing APIs
- flag unsafe combinations such as:
  - DTD processing enabled without restriction
  - resolver usage that permits external entity resolution

## Safe Patterns To Recognize

- `DtdProcessing = Prohibit`
- `XmlResolver = null`
- secure `XmlReaderSettings` usage

## Suggested Implementation

- start with configuration-pattern detection
- report only when unsafe parser settings are visible in code
- optionally add taint/context later if needed

## Dependencies

- Feature 02 tests

## Acceptance Criteria

- flags unsafe XML parser settings
- avoids flagging clearly safe parser configurations
- maps findings to `CWE-611`

## Dissertation Value

This rule is easy to explain, easy to demo, and strengthens the secure-API-misuse coverage of the analyzer.
