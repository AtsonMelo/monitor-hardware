using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using LibreHardwareMonitor.Hardware;

class TechnicalReportResult
{
    public string ReportPath { get; set; } = "";
    public string Content { get; set; } = "";
}

static class TechnicalReportService
{
    public static TechnicalReportResult Create(AppConfig? config = null)
    {
        Directory.CreateDirectory(AppLogService.LogDirectory);

        string reportPath = Path.Combine(
            AppLogService.LogDirectory,
            $"technical-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        string content = BuildReport(config);

        File.WriteAllText(reportPath, content, Encoding.UTF8);

        return new TechnicalReportResult
        {
            ReportPath = reportPath,
            Content = content
        };
    }

    private static string BuildReport(AppConfig? config)
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("Monitor Hardware - Relatorio Tecnico");
        report.AppendLine("=====================================");
        report.AppendLine();
        report.AppendLine($"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Versao do app: {GetAppVersion()}");
        report.AppendLine($"Windows: {RuntimeInformation.OSDescription}");
        report.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        report.AppendLine($"Arquitetura SO/processo: {RuntimeInformation.OSArchitecture}/{RuntimeInformation.ProcessArchitecture}");
        report.AppendLine($"Executando como administrador: {IsRunningAsAdministrator()}");
        report.AppendLine($"Diretorio do log: {AppLogService.LogDirectory}");
        report.AppendLine();

        AppendConfig(report, config);
        AppendSensors(report);

        return report.ToString();
    }

    private static void AppendConfig(StringBuilder report, AppConfig? config)
    {
        report.AppendLine("Configuracao");
        report.AppendLine("------------");

        if (config == null)
        {
            report.AppendLine("Config nao informada.");
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

    private static void AppendSensors(StringBuilder report)
    {
        report.AppendLine("Sensores");
        report.AppendLine("--------");

        try
        {
            HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensorsWithWarmup();

            AppendSummary(report, sensors);
            AppendHardwareGroups(report, sensors);
            AppendDetailedSensors(report, sensors);
        }
        catch (Exception ex)
        {
            report.AppendLine("Nao foi possivel ler os sensores.");
            report.AppendLine(ex.ToString());
        }

        report.AppendLine();
    }

    private static void AppendSummary(StringBuilder report, List<SensorReading> sensors)
    {
        int total = sensors.Count;
        int withValue = sensors.Count(sensor => sensor.Value.HasValue);
        int withoutValue = total - withValue;

        report.AppendLine("Resumo");
        report.AppendLine("------");
        report.AppendLine($"Total de sensores: {total}");
        report.AppendLine($"Sensores com valor: {withValue}");
        report.AppendLine($"Sensores sem valor: {withoutValue}");
        report.AppendLine($"Temperaturas: {sensors.Count(sensor => sensor.SensorType == SensorType.Temperature)}");
        report.AppendLine($"Fans: {sensors.Count(sensor => sensor.SensorType == SensorType.Fan)}");
        report.AppendLine($"GPUs: {sensors.Count(sensor => sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase))}");
        report.AppendLine($"Storages: {sensors.Count(sensor => sensor.HardwareType == HardwareType.Storage)}");
        report.AppendLine($"Rede: {sensors.Count(sensor => sensor.HardwareType == HardwareType.Network)}");
        report.AppendLine($"Bateria: {sensors.Count(sensor => sensor.HardwareType.ToString().Contains("Battery", StringComparison.OrdinalIgnoreCase))}");
        report.AppendLine();

        AppendDetectedHardware(report, "GPUs detectadas", sensors, sensor => sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase));
        AppendDetectedHardware(report, "Storages detectados", sensors, sensor => sensor.HardwareType == HardwareType.Storage);
        AppendDetectedHardware(report, "Baterias detectadas", sensors, sensor => sensor.HardwareType.ToString().Contains("Battery", StringComparison.OrdinalIgnoreCase));
        AppendDetectedHardware(report, "Adaptadores de rede detectados", sensors, sensor => sensor.HardwareType == HardwareType.Network);
    }

    private static void AppendDetectedHardware(
        StringBuilder report,
        string title,
        List<SensorReading> sensors,
        Func<SensorReading, bool> predicate)
    {
        List<string> names = sensors
            .Where(predicate)
            .Select(sensor => $"{sensor.HardwareType}: {sensor.HardwareName}")
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        report.AppendLine(title);
        report.AppendLine(new string('-', title.Length));

        if (names.Count == 0)
        {
            report.AppendLine("Nenhum.");
        }
        else
        {
            foreach (string name in names)
            {
                report.AppendLine($"- {name}");
            }
        }

        report.AppendLine();
    }

    private static void AppendHardwareGroups(StringBuilder report, List<SensorReading> sensors)
    {
        report.AppendLine("Hardware detectado");
        report.AppendLine("------------------");

        foreach (var group in sensors
                     .GroupBy(sensor => new
                     {
                         sensor.HardwareType,
                         sensor.HardwareName,
                         sensor.HardwareIdentifier
                     })
                     .OrderBy(group => group.Key.HardwareType.ToString())
                     .ThenBy(group => group.Key.HardwareName))
        {
            int withValue = group.Count(sensor => sensor.Value.HasValue);
            int withoutValue = group.Count() - withValue;

            report.AppendLine($"- {group.Key.HardwareType}: {group.Key.HardwareName}");
            report.AppendLine($"  ID: {group.Key.HardwareIdentifier}");
            report.AppendLine($"  Sensores: {group.Count()} | Com valor: {withValue} | Sem valor: {withoutValue}");
        }

        report.AppendLine();
    }

    private static void AppendDetailedSensors(StringBuilder report, List<SensorReading> sensors)
    {
        report.AppendLine("Detalhamento dos sensores");
        report.AppendLine("-------------------------");

        foreach (var group in sensors
                     .GroupBy(sensor => new
                     {
                         sensor.HardwareType,
                         sensor.HardwareName,
                         sensor.HardwareIdentifier
                     })
                     .OrderBy(group => group.Key.HardwareType.ToString())
                     .ThenBy(group => group.Key.HardwareName))
        {
            report.AppendLine();
            report.AppendLine($"{group.Key.HardwareType}: {group.Key.HardwareName}");
            report.AppendLine($"ID: {group.Key.HardwareIdentifier}");

            foreach (SensorReading sensor in group
                         .OrderBy(sensor => sensor.SensorType.ToString())
                         .ThenBy(sensor => sensor.SensorName))
            {
                report.AppendLine(
                    $"  - {sensor.SensorType} | {sensor.SensorName} | " +
                    $"Valor: {FormatValue(sensor.Value)} | " +
                    $"Min: {FormatValue(sensor.Min)} | " +
                    $"Max: {FormatValue(sensor.Max)} | " +
                    $"ID: {sensor.SensorIdentifier}");
            }
        }

        report.AppendLine();
    }

    private static string FormatValue(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.###")
            : "sem valor";
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly()
                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
               ?? "desconhecida";
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}