using System.Text.Json.Serialization;

namespace Analyzer.Reporting.Sarif;

// Minimal SARIF 2.1.0 model (enough for GitHub/Azure DevOps ingestion)
public sealed class SarifLog
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "2.1.0";

    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://json.schemastore.org/sarif-2.1.0.json";

    [JsonPropertyName("runs")]
    public List<SarifRun> Runs { get; init; } = new();
}

public sealed class SarifRun
{
    [JsonPropertyName("tool")]
    public SarifTool Tool { get; init; } = new();

    [JsonPropertyName("results")]
    public List<SarifResult> Results { get; init; } = new();
}

public sealed class SarifTool
{
    [JsonPropertyName("driver")]
    public SarifDriver Driver { get; init; } = new();
}

public sealed class SarifDriver
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "AI Static Security Analyzer";

    [JsonPropertyName("informationUri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InformationUri { get; init; }

    [JsonPropertyName("rules")]
    public List<SarifRule> Rules { get; init; } = new();
}

public sealed class SarifRule
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("shortDescription")]
    public SarifMultiformatMessageString ShortDescription { get; init; } = new();

    [JsonPropertyName("fullDescription")]
    public SarifMultiformatMessageString? FullDescription { get; init; }

    [JsonPropertyName("help")]
    public SarifMultiformatMessageString? Help { get; init; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }
}

public sealed class SarifResult
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; init; } = string.Empty;

    // "error" | "warning" | "note" | "none"
    [JsonPropertyName("level")]
    public string Level { get; init; } = "warning";

    [JsonPropertyName("message")]
    public SarifMessage Message { get; init; } = new();

    [JsonPropertyName("locations")]
    public List<SarifLocation> Locations { get; init; } = new();

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }
}

public sealed class SarifMessage
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

public sealed class SarifLocation
{
    [JsonPropertyName("physicalLocation")]
    public SarifPhysicalLocation PhysicalLocation { get; init; } = new();
}

public sealed class SarifPhysicalLocation
{
    [JsonPropertyName("artifactLocation")]
    public SarifArtifactLocation ArtifactLocation { get; init; } = new();

    [JsonPropertyName("region")]
    public SarifRegion Region { get; init; } = new();
}

public sealed class SarifArtifactLocation
{
    [JsonPropertyName("uri")]
    public string Uri { get; init; } = string.Empty;
}

public sealed class SarifRegion
{
    [JsonPropertyName("startLine")]
    public int StartLine { get; init; }

    [JsonPropertyName("startColumn")]
    public int StartColumn { get; init; }
}

public sealed class SarifMultiformatMessageString
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}