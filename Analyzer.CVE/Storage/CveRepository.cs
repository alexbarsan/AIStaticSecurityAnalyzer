using Microsoft.Data.Sqlite;

namespace Analyzer.CVE.Storage;

public sealed class CveRepository
{
    private readonly string _dbPath;

    public CveRepository(string dbPath) => _dbPath = dbPath;

    public void Upsert(
        string cveId,
        string? cweId,
        string? published,
        string? lastModified,
        string? description,
        double? cvssScore,
        string? cvssVector,
        string? cvssSeverity)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        INSERT INTO cves (cve_id, cwe_id, published, last_modified, description, cvss_base_score, cvss_vector, cvss_severity)
        VALUES ($cve_id, $cwe_id, $published, $last_modified, $description, $cvss_base_score, $cvss_vector, $cvss_severity)
        ON CONFLICT(cve_id) DO UPDATE SET
            cwe_id = excluded.cwe_id,
            published = excluded.published,
            last_modified = excluded.last_modified,
            description = excluded.description,
            cvss_base_score = excluded.cvss_base_score,
            cvss_vector = excluded.cvss_vector,
            cvss_severity = excluded.cvss_severity;
        """;

        cmd.Parameters.AddWithValue("$cve_id", cveId);
        cmd.Parameters.AddWithValue("$cwe_id", (object?)cweId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$published", (object?)published ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$last_modified", (object?)lastModified ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$description", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cvss_base_score", (object?)cvssScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cvss_vector", (object?)cvssVector ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cvss_severity", (object?)cvssSeverity ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public (string cveId, double? score, string? severity)? GetBestMatchByCwe(string cweId)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
        SELECT cve_id, cvss_base_score, cvss_severity
        FROM cves
        WHERE cwe_id = $cwe_id
        ORDER BY 
            CASE WHEN cvss_base_score IS NULL THEN 1 ELSE 0 END,
            cvss_base_score DESC,
            last_modified DESC
        LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$cwe_id", cweId);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return (r.GetString(0),
                r.IsDBNull(1) ? null : r.GetDouble(1),
                r.IsDBNull(2) ? null : r.GetString(2));
    }
}
