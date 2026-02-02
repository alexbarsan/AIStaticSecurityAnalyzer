using System.Net.Http.Headers;
using System.Text.Json;

namespace Analyzer.CVE.Nvd;

public sealed class NvdClient
{
    private readonly HttpClient _http;

    // NVD 2.0 base URL (CVE endpoint). :contentReference[oaicite:3]{index=3}
    private const string BaseUrl = "https://services.nvd.nist.gov/rest/json/cves/2.0";

    public NvdClient(HttpClient httpClient, string? apiKey)
    {
        _http = httpClient;

        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // API key recommended to raise rate limits. :contentReference[oaicite:4]{index=4}
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Add("apiKey", apiKey);
    }

    public async Task<JsonDocument> GetCvesModifiedAsync(DateTimeOffset modStart, DateTimeOffset modEnd, int startIndex, int resultsPerPage, CancellationToken ct)
    {
        // Best practice: query by modified date windows. :contentReference[oaicite:5]{index=5}
        var url =
            $"{BaseUrl}?lastModStartDate={Uri.EscapeDataString(modStart.ToString("o"))}" +
            $"&lastModEndDate={Uri.EscapeDataString(modEnd.ToString("o"))}" +
            $"&startIndex={startIndex}&resultsPerPage={resultsPerPage}";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
}
