# Codex Context - AI Static Security Analyzer v2

This root file exists for convenience and follows the same pattern as the original context file.

The canonical updated context is:

- [docs/CODEX_CONTEXT_V2.md](docs/CODEX_CONTEXT_V2.md)

Use that file as the primary source for future implementation sessions. It reflects the current repository more accurately than the older `codex_context_ai_static_security_analyzer.md` file and includes:

- confirmed implementation state
- verified code/context drift
- the recommended next 10 features
- architectural guidance for future Codex work
- the rule that feature work should begin with tests first whenever feasible
- the rule that each meaningful feature slice should produce an academic log markdown file
- the current Feature 01 status, including recursive basic `ProjectReference` handling
- the fact that project scan scope is now based on evaluated MSBuild items for `.csproj` inputs
