using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/AtsonMelo/monitor-hardware/releases/latest";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public Version CurrentVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        JsonElement root = document.RootElement;
        string tagName = root.GetProperty("tag_name").GetString() ?? "";
        string releaseUrl = root.GetProperty("html_url").GetString() ?? LatestReleaseUrl;
        Version? latestVersion = ParseVersion(tagName);

        if (latestVersion == null)
        {
            return new UpdateCheckResult(false, CurrentVersion, null, releaseUrl, null);
        }

        string? downloadUrl = GetDownloadUrl(root);

        return new UpdateCheckResult(
            latestVersion > Normalize(CurrentVersion),
            Normalize(CurrentVersion),
            latestVersion,
            releaseUrl,
            downloadUrl);
    }

    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonitorHardware/1.0");

        return httpClient;
    }

    private static Version? ParseVersion(string tagName)
    {
        string normalized = tagName.Trim().TrimStart('v', 'V');

        return Version.TryParse(normalized, out Version? version)
            ? Normalize(version)
            : null;
    }

    private static Version Normalize(Version version)
    {
        int build = version.Build >= 0 ? version.Build : 0;

        return new Version(version.Major, version.Minor, build);
    }

    private static string? GetDownloadUrl(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";

            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return asset.GetProperty("browser_download_url").GetString();
            }
        }

        return null;
    }
}

record UpdateCheckResult(
    bool HasUpdate,
    Version CurrentVersion,
    Version? LatestVersion,
    string ReleaseUrl,
    string? DownloadUrl);
