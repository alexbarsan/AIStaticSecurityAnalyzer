using System.Text.Json;
using Analyzer.CVE.Storage;

namespace Analyzer.CVE.Nvd;

public sealed class NvdImporter
{
    private readonly NvdClient _client;
    private readonly CveRepository _repo;

    public NvdImporter(NvdClient client, CveRepository repo)
    {
        _client = client;
        _repo = repo;
    }

    public async Task SyncModifiedWindowAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        int startIndex = 0;
        const int pageSize = 2000; // NVD supports pagination; keep <= 2000. :contentReference[oaicite:6]{index=6}

        while (true)
        {
            using var doc = await _client.GetCvesModifiedAsync(start, end, startIndex, pageSize, ct);

            var root = doc.RootElement;

            int total = root.GetProperty("totalResults").GetInt32();
            if (!root.TryGetProperty("vulnerabilities", out var vulns) || vulns.ValueKind != JsonValueKind.Array)
                break;

            foreach (var v in vulns.EnumerateArray())
            {
                var cve = v.GetProperty("cve");
                var cveId = cve.GetProperty("id").GetString() ?? "";

                var published = cve.TryGetProperty("published", out var pub) ? pub.GetString() : null;
                var lastMod = cve.TryGetProperty("lastModified", out var lm) ? lm.GetString() : null;

                // Description (pick first English if possible)
                string? desc = null;
                if (cve.TryGetProperty("descriptions", out var descArr) && descArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var d in descArr.EnumerateArray())
                    {
                        var lang = d.TryGetProperty("lang", out var l) ? l.GetString() : null;
                        if (lang == "en")
                        {
                            desc = d.TryGetProperty("value", out var val) ? val.GetString() : null;
                            break;
                        }
                    }
                    desc ??= descArr.EnumerateArray().FirstOrDefault().TryGetProperty("value", out var v2) ? v2.GetString() : null;
                }

                // CWE (best-effort)
                string? cweId = null;
                if (cve.TryGetProperty("weaknesses", out var weakArr) && weakArr.ValueKind == JsonValueKind.Array)
                {
                    // Find first CWE like "CWE-89"
                    foreach (var w in weakArr.EnumerateArray())
                    {
                        if (!w.TryGetProperty("description", out var wdesc) || wdesc.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var wd in wdesc.EnumerateArray())
                        {
                            var val = wd.TryGetProperty("value", out var vv) ? vv.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(val) && val.StartsWith("CWE-"))
                            {
                                cweId = val;
                                break;
                            }
                        }
                        if (cweId != null) break;
                    }
                }

                // CVSS (best-effort)
                double? score = null;
                string? severity = null;
                string? vector = null;

                if (cve.TryGetProperty("metrics", out var metrics))
                {
                    // Try CVSS v3.1 first, then v3.0 (structure may vary)
                    if (TryReadCvss(metrics, "cvssMetricV31", out score, out severity, out vector) ||
                        TryReadCvss(metrics, "cvssMetricV30", out score, out severity, out vector))
                    {
                        // ok
                    }
                }

                _repo.Upsert(cveId, cweId, published, lastMod, desc, score, vector, severity);
            }

            startIndex += pageSize;
            if (startIndex >= total)
                break;
        }
    }

    private static bool TryReadCvss(JsonElement metrics, string property, out double? score, out string? severity, out string? vector)
    {
        score = null; severity = null; vector = null;

        if (!metrics.TryGetProperty(property, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            return false;

        // Take first metric entry
        var first = arr[0];

        // NVD CVSS blocks typically include a "cvssData" object
        if (!first.TryGetProperty("cvssData", out var data))
            return false;

        score = data.TryGetProperty("baseScore", out var bs) ? bs.GetDouble() : (double?)null;
        vector = data.TryGetProperty("vectorString", out var vs) ? vs.GetString() : null;
        severity = data.TryGetProperty("baseSeverity", out var sev) ? sev.GetString() : null;

        return score != null || severity != null || vector != null;
    }
}
