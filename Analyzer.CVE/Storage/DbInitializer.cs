using Microsoft.Data.Sqlite;

namespace Analyzer.CVE.Storage;

public static class DbInitializer
{
    public static void EnsureCreated(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS cves (
            cve_id TEXT PRIMARY KEY,
            cwe_id TEXT NULL,
            published TEXT NULL,
            last_modified TEXT NULL,
            description TEXT NULL,
            cvss_base_score REAL NULL,
            cvss_vector TEXT NULL,
            cvss_severity TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_cves_cwe_id ON cves(cwe_id);
        """;
        cmd.ExecuteNonQuery();
    }
}
