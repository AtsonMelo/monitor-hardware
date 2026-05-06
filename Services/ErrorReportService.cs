using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using LibreHardwareMonitor.Hardware;

class ErrorReportResult
{
    public string ReportPath { get; set; } = "";
    public string Content { get; set; } = "";
    public string GitHubUrl { get; set; } = "";
}

static class ErrorReportService
{
    public const string SupportUrl = "https://github.com/AtsonMelo/monitor-hardware/issues/new/choose";
    public const string ErrorReportUrl = "https://github.com/AtsonMelo/monitor-hardware/issues/new?template=log_report.yml";

    public static ErrorReportResult Create(AppConfig? config = null, Exception? exception = null)
    {
        Directory.CreateDirectory(AppLogService.LogDirectory);

        string reportPath = Path.Combine(
            AppLogService.LogDirectory,
            $"error-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        string content = BuildReport(config, exception);
        File.WriteAllText(reportPath, content, Encoding.UTF8);

        return new ErrorReportResult
        {
            ReportPath = reportPath,
            Content = content,
            GitHubUrl = ErrorReportUrl
        };
    }

    private static string BuildReport(AppConfig? config, Exception? exception)
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("Monitor Hardware - Relatorio de Erro");
        report.AppendLine("====================================");
        report.AppendLine();
        report.AppendLine($"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Versao do app: {GetAppVersion()}");
        report.AppendLine($"Windows: {RuntimeInformation.OSDescription}");
        report.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        report.AppendLine($"Arquitetura do SO/processo: {RuntimeInformation.OSArchitecture}/{RuntimeInformation.ProcessArchitecture}");
        report.AppendLine($"Executando como administrador: {IsRunningAsAdministrator()}");
        report.AppendLine();

        if (exception != null)
        {
            report.AppendLine("Erro capturado");
            report.AppendLine("--------------");
            report.AppendLine(Redact(exception.ToString()));
            report.AppendLine();
        }

        AppendConfig(report, config);
        AppendHardwareSnapshot(report);
        AppendRecentCsvFiles(report);
        AppendAppLog(report);

        report.AppendLine("Como enviar");
        report.AppendLine("-----------");
        report.AppendLine($"1. Abra: {ErrorReportUrl}");
        report.AppendLine("2. Cole este relatorio no campo de log.");
        report.AppendLine("3. Revise se existe algum dado pessoal antes de publicar.");

        return report.ToString();
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly()
                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
               ?? "desconhecida";
    }

    private static void AppendConfig(StringBuilder report, AppConfig? config)
    {
        report.AppendLine("Configuracao");
        report.AppendLine("------------");

        if (config == null)
        {
            report.AppendLine("Config nao carregada no momento da falha.");
            report.AppendLine();
            return;
        }

        report.AppendLine($"CpuTempMax: {config.CpuTempMax}");
        report.AppendLine($"GpuTempMax: {config.GpuTempMax}");
        report.AppendLine($"SsdTempMax: {config.SsdTempMax}");
        report.AppendLine($"IntervaloMs: {config.IntervaloMs}");
        report.AppendLine($"EnableCsv: {config.EnableCsv}");
        report.AppendLine($"EnableConsole: {config.EnableConsole}");
        report.AppendLine($"Mode: {config.Mode}");
        report.AppendLine($"CpuFanSensorName: {config.CpuFanSensorName}");
        report.AppendLine($"TemperatureUnit: {config.TemperatureUnit}");
        report.AppendLine($"ShowTemperatureUnitInTrayIcon: {config.ShowTemperatureUnitInTrayIcon}");
        report.AppendLine($"EnableAutoUpdateCheck: {config.EnableAutoUpdateCheck}");
        report.AppendLine();
    }

    private static void AppendHardwareSnapshot(StringBuilder report)
    {
        report.AppendLine("Sensores detectados");
        report.AppendLine("-------------------");

        try
        {
            HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();

            report.AppendLine($"Total de sensores: {sensors.Count}");

            foreach (var group in sensors
                         .GroupBy(sensor => new { sensor.HardwareType, sensor.HardwareName })
                         .OrderBy(group => group.Key.HardwareType.ToString())
                         .ThenBy(group => group.Key.HardwareName))
            {
                report.AppendLine($"- {group.Key.HardwareType}: {group.Key.HardwareName} ({group.Count()} sensores)");
            }

            report.AppendLine();
            report.AppendLine("Sensores principais:");

            foreach (SensorReading sensor in sensors
                         .Where(IsUsefulForReport)
                         .OrderBy(sensor => sensor.HardwareType.ToString())
                         .ThenBy(sensor => sensor.HardwareName)
                         .ThenBy(sensor => sensor.SensorType.ToString())
                         .ThenBy(sensor => sensor.SensorName)
                         .Take(120))
            {
                report.AppendLine($"- {sensor.HardwareType} | {sensor.HardwareName} | {sensor.SensorType} | {sensor.SensorName}: {sensor.Value:0.0}");
            }
        }
        catch (Exception ex)
        {
            report.AppendLine("Nao foi possivel ler sensores durante a geracao do relatorio.");
            report.AppendLine(Redact(ex.Message));
        }

        report.AppendLine();
    }

    private static bool IsUsefulForReport(SensorReading sensor)
    {
        return sensor.Value.HasValue &&
               sensor.SensorType is SensorType.Temperature or
                   SensorType.Load or
                   SensorType.Power or
                   SensorType.Fan or
                   SensorType.Clock or
                   SensorType.Data or
                   SensorType.Throughput or
                   SensorType.Level;
    }

    private static void AppendRecentCsvFiles(StringBuilder report)
    {
        report.AppendLine("Arquivos CSV recentes");
        report.AppendLine("---------------------");

        try
        {
            string logsDirectory = Path.GetFullPath("logs");

            if (!Directory.Exists(logsDirectory))
            {
                report.AppendLine("Pasta logs/ nao encontrada no diretorio atual do app.");
                report.AppendLine();
                return;
            }

            foreach (FileInfo file in new DirectoryInfo(logsDirectory)
                         .GetFiles("monitor-hardware-*.csv")
                         .OrderByDescending(file => file.LastWriteTime)
                         .Take(5))
            {
                report.AppendLine($"- {file.Name} | {file.Length} bytes | {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"Nao foi possivel listar CSVs: {Redact(ex.Message)}");
        }

        report.AppendLine();
    }

    private static void AppendAppLog(StringBuilder report)
    {
        report.AppendLine("app.log");
        report.AppendLine("-------");

        if (!File.Exists(AppLogService.LogPath))
        {
            report.AppendLine("Arquivo app.log ainda nao existe.");
            report.AppendLine();
            return;
        }

        string logContent = Redact(File.ReadAllText(AppLogService.LogPath, Encoding.UTF8));
        const int maxCharacters = 12000;

        if (logContent.Length > maxCharacters)
        {
            logContent = logContent[^maxCharacters..];
            report.AppendLine($"Mostrando apenas os ultimos {maxCharacters} caracteres.");
        }

        report.AppendLine(logContent);
        report.AppendLine();
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string Redact(string text)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            text = text.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }

        string userName = Environment.UserName;

        if (!string.IsNullOrWhiteSpace(userName))
        {
            text = text.Replace(userName, "SEU_USUARIO", StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}
