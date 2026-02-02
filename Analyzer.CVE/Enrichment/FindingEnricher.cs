using Analyzer.Core.Models;
using Analyzer.CVE.Storage;

namespace Analyzer.CVE.Enrichment;

public sealed class FindingEnricher
{
    private readonly CveRepository _repo;

    public FindingEnricher(CveRepository repo) => _repo = repo;

    public void Enrich(IReadOnlyCollection<Finding> findings)
    {
        foreach (var f in findings)
        {
            if (string.IsNullOrWhiteSpace(f.Vulnerability.CWEId))
                continue;

            // If already enriched, skip
            if (!string.IsNullOrWhiteSpace(f.CveId))
                continue;

            var best = _repo.GetBestMatchByCwe(f.Vulnerability.CWEId);
            if (best == null) continue;

            f.CveId = best.Value.cveId;
            f.CvssBaseScore = best.Value.score;
            f.CvssSeverity = best.Value.severity;
        }
    }
}
