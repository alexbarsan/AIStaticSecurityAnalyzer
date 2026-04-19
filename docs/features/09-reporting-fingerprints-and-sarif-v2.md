# Feature 09 - Reporting v2, Stable Fingerprints, And SARIF Improvements

## Goal

Improve the quality and stability of finding outputs so the analyzer integrates better with CI/CD platforms and produces more trustworthy result tracking.

## Why It Matters

Once more rules are added, result stability becomes important. Without finding fingerprints and stronger metadata, suppressions, baselines, and code-scanning UX will be weak.

## Scope

- add a stable finding fingerprint
- enrich finding metadata with:
  - rule id
  - CWE
  - optional CVE/CVSS
  - confidence
  - remediation
  - evidence snippet
- improve SARIF properties and taxonomy mapping
- optionally add markdown or HTML summary output later, but not in v1

## Suggested Fingerprint Inputs

- rule id
- normalized file path
- line or symbol anchor
- normalized sink/source signature where available

## Suggested SARIF Improvements

- consistent rule ids
- stable properties for suppressions and baselines
- cleaner URI/path handling
- richer help text and remediation guidance

## Dependencies

- supports Feature 08 suppressions and baseline mode

## Acceptance Criteria

- the same finding produces the same fingerprint across repeated scans when code is unchanged
- JSON and SARIF contain consistent metadata
- reports remain valid for GitHub Code Scanning upload

## Dissertation Value

This feature improves the professionalism and traceability of the project without changing its conceptual scope.
