**🔐 AI Static Security Analyzer**
A modular .NET-based static application security testing (SAST) tool that combines rule-based analysis, CVE/CVSS enrichment, and machine learning–based false-positive reduction.

**📖 Overview**
AI Static Security Analyzer is a research-oriented security analysis tool developed as part of a master’s thesis in Cybersecurity.

It performs:
🔎 Static code analysis using Roslyn
🛡 Detection of security vulnerabilities (CWE-based)
🌍 CVE + CVSS enrichment from NVD
🤖 AI-based confidence scoring using ML.NET
📊 SARIF reporting compatible with GitHub Code Scanning
🚦 CI/CD security gating

The system is modular and extensible, designed to demonstrate how modern SAST tools can integrate traditional rule engines with AI-driven prioritization.

**🏗 Architecture**
The solution is split into multiple projects:
Analyzer.Core        → Models & shared domain objects
Analyzer.Roslyn      → Static analysis engine (rule-based)
Analyzer.CVE         → NVD integration + SQLite storage
Analyzer.AI          → ML.NET-based false-positive classifier
Analyzer.Reporting   → JSON & SARIF exporters
Analyzer.CLI         → Command-line interface


**🚀 Usage**
1. Scan project
dotnet run --project Analyzer.CLI -- ./ --json

2. Enable AI scoring
dotnet run --project Analyzer.CLI -- ./ --ai --min-confidence 0.7

3. Export SARIF (for GitHub Code Scanning)
dotnet run --project Analyzer.CLI -- ./ --sarif analysis.sarif.json

4. Enforce security gate (CI mode)
dotnet run --project Analyzer.CLI -- ./ --fail-on high

Exit codes:
0 → no blocking findings
2 → findings at or above threshold

**🌍 CVE Enrichment (NVD Integration)**
To enable CVE enrichment:
Request free NVD API key
https://nvd.nist.gov/developers/request-an-api-key
Set environment variable:
Windows:
setx NVD_API_KEY "your_key_here"
Linux/macOS:
export NVD_API_KEY=your_key_here

**Sync CVEs:**
dotnet run --project Analyzer.CLI -- sync-nvd --days 7
CVE data is stored locally in SQLite (cves.db).

**📊 GitHub Code Scanning Integration**
The project supports SARIF 2.1.0 output and automatic upload to GitHub Code Scanning via GitHub Actions.
Results appear under:
Security → Code scanning alerts

**The workflow:**
Builds the solution
Runs analyzer
Uploads SARIF
Optionally fails the build if High/Critical issues are detected

**🧪 Example Output**
[Critical] Hardcoded secret in source code
(conf 1.00) (CVE-2025-69971, CVSS 9.8)
at TempForTests/Test.cs:14
[High] Use of weak cryptographic hashing algorithm
(conf 1.00) (CVE-2026-21718, CVSS 10.0)
at TempForTests/Test.cs:16

**📦 Requirements**
.NET 8.0 SDK
Optional: NVD API Key
Optional: GitHub Actions for SARIF integration

**🧠 Research Goals**
This project demonstrates:
Integration of rule-based SAST with ML-based prioritization
Practical CVE/CVSS enrichment workflows
Secure CI/CD gating
SARIF standard compliance
Continuous supervised learning via exported training datasets

**🔮 Future Improvements**
More vulnerability rules (SQL injection, XSS, insecure deserialization)
Dataflow analysis
Inter-procedural analysis
Transformer-based ML models
Web UI dashboard
Containerized deployment
Multi-language support

**📜 License**
MIT License

**👨‍💻 Author**
Alex Bârsan
Master’s Program – ISM IT&C Cybersecurity
Academy of Economic Studies (ASE)
