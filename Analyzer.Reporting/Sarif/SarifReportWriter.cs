using System.Text.Json;
using Analyzer.Core.Models;

namespace Analyzer.Reporting.Sarif;

public sealed class SarifReportWriter
{
    public void Write(string outputPath, string projectPath, IReadOnlyCollection<Finding> findings)
    {
        var log = new SarifLog();

        var run = new SarifRun
        {
            Tool = new SarifTool
            {
                Driver = new SarifDriver
                {
                    Name = "AI Static Security Analyzer",
                    InformationUri = null,
                    Rules = BuildRules(findings)
                }
            },
            Results = BuildResults(projectPath, findings)
        };

        log.Runs.Add(run);

        var json = JsonSerializer.Serialize(log, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(outputPath, json);
    }

    private static List<SarifRule> BuildRules(IReadOnlyCollection<Finding> findings)
    {
        // Deduplicate by Vulnerability.Id (your internal id)
        return findings
            .Select(f => f.Vulnerability)
            .GroupBy(v => v.Id)
            .Select(g =>
            {
                var v = g.First();
                var props = new Dictionary<string, object?>
                {
                    ["cweId"] = v.CWEId,
                    ["defaultSeverity"] = v.Severity.ToString(),
                };

                // optional: CVE might be set at vulnerability-level later; for now findings carry it
                // keep properties simple and stable

                return new SarifRule
                {
                    Id = v.Id,
                    Name = v.Name,
                    ShortDescription = new SarifMultiformatMessageString { Text = v.Name },
                    FullDescription = string.IsNullOrWhiteSpace(v.Description) ? null : new SarifMultiformatMessageString { Text = v.Description },
                    Help = string.IsNullOrWhiteSpace(v.Recommandation) ? null : new SarifMultiformatMessageString { Text = v.Recommandation },
                    Properties = props!
                };
            })
            .ToList();
    }

    private static List<SarifResult> BuildResults(string projectPath, IReadOnlyCollection<Finding> findings)
    {
        return findings.Select(f =>
        {
            var level = MapSeverityToSarifLevel(f.Vulnerability.Severity);

            // Use relative path if possible (better UX in platforms)
            var uri = ToSarifUri(projectPath, f.FilePath);

            // Include enrichment (CVE/CVSS/Confidence) in properties
            var props = new Dictionary<string, object?>
            {
                ["cweId"] = f.Vulnerability.CWEId,
                ["confidence"] = f.Confidence,
                ["cveId"] = f.CveId,
                ["cvssBaseScore"] = f.CvssBaseScore,
                ["cvssSeverity"] = f.CvssSeverity
            };

            // Message includes a compact summary
            var msg = $"{f.Vulnerability.Name}";
            if (!string.IsNullOrWhiteSpace(f.CveId))
                msg += $" ({f.CveId})";
            if (f.CvssBaseScore is not null)
                msg += $" CVSS {f.CvssBaseScore:0.0}";
            msg += $" [conf {f.Confidence:0.00}]";

            return new SarifResult
            {
                RuleId = f.Vulnerability.Id,
                Level = level,
                Message = new SarifMessage { Text = msg },
                Locations = new List<SarifLocation>
                {
                    new SarifLocation
                    {
                        PhysicalLocation = new SarifPhysicalLocation
                        {
                            ArtifactLocation = new SarifArtifactLocation { Uri = uri },
                            Region = new SarifRegion
                            {
                                StartLine = Math.Max(1, f.Line),
                                StartColumn = Math.Max(1, f.Column)
                            }
                        }
                    }
                },
                Properties = props!
            };
        }).ToList();
    }

    private static string MapSeverityToSarifLevel(Severity severity) =>
        severity switch
        {
            Severity.Critical => "error",
            Severity.High => "error",
            Severity.Medium => "warning",
            Severity.Low => "note",
            Severity.Info => "note",
            _ => "warning"
        };

    private static string ToSarifUri(string projectPath, string filePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(projectPath) &&
                Path.IsPathFullyQualified(projectPath) &&
                Path.IsPathFullyQualified(filePath))
            {
                var rel = Path.GetRelativePath(projectPath, filePath);
                // SARIF expects forward slashes in URIs
                return rel.Replace('\\', '/');
            }
        }
        catch { /* ignore and fallback */ }

        return filePath.Replace('\\', '/');
    }
}