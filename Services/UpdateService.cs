using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text;

public class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/AtsonMelo/monitor-hardware/releases/latest";
    private const string AppExecutableName = "monitor-hardware.exe";
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

    public async Task StartUpdateAsync(
        UpdateCheckResult update,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!update.HasUpdate)
        {
            throw new InvalidOperationException("Não há atualização disponível.");
        }

        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
        {
            throw new InvalidOperationException("A release não possui um pacote ZIP para download.");
        }

        string updateDirectory = CreateUpdateDirectory(update);
        string zipPath = Path.Combine(updateDirectory, "monitor-hardware-update.zip");
        string extractDirectory = Path.Combine(updateDirectory, "extracted");

        progress?.Report("Baixando...");
        await DownloadFileAsync(update.DownloadUrl, zipPath, cancellationToken);

        progress?.Report("Extraindo...");
        ZipFile.ExtractToDirectory(zipPath, extractDirectory, overwriteFiles: true);

        string payloadDirectory = FindPayloadDirectory(extractDirectory);
        string appDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string restartExecutablePath = Path.Combine(appDirectory, AppExecutableName);
        string scriptPath = Path.Combine(updateDirectory, "aplicar-atualizacao.ps1");

        progress?.Report("Preparando...");
        string script = BuildInstallerScript(
            Environment.ProcessId,
            appDirectory,
            payloadDirectory,
            restartExecutablePath);

        await File.WriteAllTextAsync(scriptPath, script, Encoding.UTF8, cancellationToken);

        progress?.Report("Aplicando...");
        StartInstallerScript(scriptPath);
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonitorHardware/1.0");

        return httpClient;
    }

    private static string CreateUpdateDirectory(UpdateCheckResult update)
    {
        string version = update.LatestVersion?.ToString() ?? DateTime.Now.ToString("yyyyMMddHHmmss");
        string updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MonitorHardware",
            "updates",
            version);

        if (Directory.Exists(updateDirectory))
        {
            Directory.Delete(updateDirectory, recursive: true);
        }

        Directory.CreateDirectory(updateDirectory);
        Directory.CreateDirectory(Path.Combine(updateDirectory, "extracted"));

        return updateDirectory;
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream inputStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream outputStream = File.Create(destinationPath);

        await inputStream.CopyToAsync(outputStream, cancellationToken);
    }

    public static string FindPayloadDirectory(string extractDirectory)
    {
        string rootExecutable = Path.Combine(extractDirectory, AppExecutableName);

        if (File.Exists(rootExecutable))
        {
            return extractDirectory;
        }

        string? executablePath = Directory
            .EnumerateFiles(extractDirectory, AppExecutableName, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new FileNotFoundException($"O pacote baixado não contém {AppExecutableName}.");
        }

        return Path.GetDirectoryName(executablePath)
            ?? throw new DirectoryNotFoundException("Não foi possível localizar a pasta da nova versão.");
    }

    private static string BuildInstallerScript(
    int currentProcessId,
    string appDirectory,
    string payloadDirectory,
    string restartExecutablePath)
    {
        string updateLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MonitorHardware",
            "logs",
            "update-installer.log");

        string escapedAppDirectory = EscapePowerShellString(appDirectory);
        string escapedPayloadDirectory = EscapePowerShellString(payloadDirectory);
        string escapedRestartExecutablePath = EscapePowerShellString(restartExecutablePath);
        string escapedUpdateLogPath = EscapePowerShellString(updateLogPath);

        return $$"""
    $ErrorActionPreference = 'Stop'

    $targetProcessId = {{currentProcessId}}
    $appDirectory = '{{escapedAppDirectory}}'
    $payloadDirectory = '{{escapedPayloadDirectory}}'
    $restartExecutablePath = '{{escapedRestartExecutablePath}}'
    $updateLogPath = '{{escapedUpdateLogPath}}'
    $excludedNames = @('config.json', 'logs', 'updates')
    $maxCopyAttempts = 10

    function Write-UpdateLog {
        param([string]$Message)

        $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Add-Content -LiteralPath $updateLogPath -Value "[$timestamp] $Message"
    }

    function Copy-WithRetry {
        param(
            [string]$Source,
            [string]$Destination
        )

        for ($attempt = 1; $attempt -le $maxCopyAttempts; $attempt++) {
            try {
                Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force -ErrorAction Stop
                Write-UpdateLog "Copiado: $Source -> $Destination"
                return
            } catch {
                Write-UpdateLog "Falha ao copiar tentativa $attempt/$maxCopyAttempts: $Source -> $Destination | $($_.Exception.Message)"
                Start-Sleep -Milliseconds 800
            }
        }

        throw "Nao foi possivel copiar '$Source' para '$Destination' apos $maxCopyAttempts tentativas."
    }

    try {
        New-Item -ItemType Directory -Path (Split-Path -Path $updateLogPath -Parent) -Force | Out-Null

        Write-UpdateLog "==== Iniciando aplicacao de atualizacao ===="
        Write-UpdateLog "PID alvo: $targetProcessId"
        Write-UpdateLog "AppDirectory: $appDirectory"
        Write-UpdateLog "PayloadDirectory: $payloadDirectory"
        Write-UpdateLog "RestartExecutablePath: $restartExecutablePath"

        if (!(Test-Path -LiteralPath $payloadDirectory)) {
            throw "Payload nao encontrado: $payloadDirectory"
        }

        if (!(Test-Path -LiteralPath $appDirectory)) {
            New-Item -ItemType Directory -Path $appDirectory -Force | Out-Null
            Write-UpdateLog "Pasta do aplicativo criada: $appDirectory"
        }

        $targetProcess = Get-Process -Id $targetProcessId -ErrorAction SilentlyContinue

        if ($targetProcess) {
            Write-UpdateLog "Aguardando encerramento do processo alvo..."
            Wait-Process -Id $targetProcessId -Timeout 20 -ErrorAction SilentlyContinue
        }

        $targetProcess = Get-Process -Id $targetProcessId -ErrorAction SilentlyContinue

        if ($targetProcess) {
            Write-UpdateLog "Processo alvo ainda ativo. Forcando encerramento..."
            Stop-Process -Id $targetProcessId -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
        }

        Get-Process monitor-hardware -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Path -and
            ([System.IO.Path]::GetFullPath($_.Path).TrimEnd('\') -ieq [System.IO.Path]::GetFullPath($restartExecutablePath).TrimEnd('\'))
        } |
        ForEach-Object {
            Write-UpdateLog "Finalizando instancia extra do Monitor Hardware: PID $($_.Id)"
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }

        Get-ChildItem -LiteralPath $payloadDirectory -Force | ForEach-Object {
            if ($excludedNames -notcontains $_.Name) {
                $destination = Join-Path -Path $appDirectory -ChildPath $_.Name
                Copy-WithRetry -Source $_.FullName -Destination $destination
            } else {
                Write-UpdateLog "Preservado ou ignorado: $($_.Name)"
            }
        }

        $version = (Get-Item -LiteralPath $restartExecutablePath).VersionInfo.ProductVersion
        Write-UpdateLog "Atualizacao aplicada. Versao do executavel: $version"

        Write-UpdateLog "Reabrindo aplicativo..."
        Start-Process -FilePath $restartExecutablePath -ArgumentList '--gui'

        Write-UpdateLog "==== Atualizacao finalizada ===="
    } catch {
        Write-UpdateLog "ERRO: $($_.Exception.ToString())"
        Start-Process -FilePath "notepad.exe" -ArgumentList @($updateLogPath)
        exit 1
    }
    """;
    }

    private static string EscapePowerShellString(string value)
    {
        return value.Replace("'", "''");
    }

    private static void StartInstallerScript(string scriptPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
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

public record UpdateCheckResult(
    bool HasUpdate,
    Version CurrentVersion,
    Version? LatestVersion,
    string ReleaseUrl,
    string? DownloadUrl);
