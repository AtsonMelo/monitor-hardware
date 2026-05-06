using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using LibreHardwareMonitor.Hardware;

public class TechnicalReportResult
{
    public string ReportPath { get; set; } = "";
    public string Content { get; set; } = "";
}

public static class TechnicalReportService
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
        report.AppendLine($"GPUs: {CountDistinctHardware(sensors, IsGpuSensor)}");
        report.AppendLine($"Storages: {CountDistinctHardware(sensors, sensor => sensor.HardwareType == HardwareType.Storage)}");
        report.AppendLine($"Rede: {CountDistinctHardware(sensors, sensor => sensor.HardwareType == HardwareType.Network)}");
        report.AppendLine($"Bateria: {CountDistinctHardware(sensors, IsBatterySensor)}");
        report.AppendLine();

        AppendDetectedHardware(report, "GPUs detectadas", sensors, IsGpuSensor);
        AppendDetectedHardware(report, "Storages detectados", sensors, sensor => sensor.HardwareType == HardwareType.Storage);
        AppendDetectedHardware(report, "Baterias detectadas", sensors, IsBatterySensor);
        AppendDetectedHardware(report, "Adaptadores de rede detectados", sensors, sensor => sensor.HardwareType == HardwareType.Network);
    }

    private static void AppendSensors(StringBuilder report)
    {
        report.AppendLine("Sensores");
        report.AppendLine("--------");

        try
        {
            using HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensorsWithWarmup();

            AppendSummary(report, sensors);
            AppendFanMap(report, sensors);
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

    private static void AppendFanMap(StringBuilder report, List<SensorReading> sensors)
    {
        List<SensorReading> fanSensors = sensors
            .Where(sensor => sensor.SensorType == SensorType.Fan)
            .OrderBy(sensor => sensor.HardwareType.ToString())
            .ThenBy(sensor => sensor.HardwareName)
            .ThenBy(sensor => sensor.SensorName)
            .ToList();

        List<SensorReading> fanControls = sensors
            .Where(sensor =>
                sensor.SensorType == SensorType.Control &&
                sensor.SensorName.Contains("Fan", StringComparison.OrdinalIgnoreCase))
            .OrderBy(sensor => sensor.HardwareType.ToString())
            .ThenBy(sensor => sensor.HardwareName)
            .ThenBy(sensor => sensor.SensorName)
            .ToList();

        report.AppendLine("Mapa de fans detectadas");
        report.AppendLine("-----------------------");
        report.AppendLine($"Sensores de rotacao encontrados: {fanSensors.Count}");
        report.AppendLine($"Sensores com RPM maior que zero: {fanSensors.Count(sensor => sensor.Value.HasValue && sensor.Value.Value > 0)}");
        report.AppendLine($"Controles de fan encontrados: {fanControls.Count}");
        report.AppendLine();

        if (fanSensors.Count == 0 && fanControls.Count == 0)
        {
            report.AppendLine("Nenhum sensor ou controle de fan foi detectado.");
            report.AppendLine("Isso pode ocorrer em notebooks, hubs SATA/Molex ou controladoras que nao expoem RPM ao sistema.");
            report.AppendLine();
            return;
        }

        AppendFanSection(
            report,
            "Fans da GPU",
            fanSensors.Where(IsGpuSensor).ToList());

        AppendFanSection(
            report,
            "Fans da placa-mae / SuperIO",
            fanSensors.Where(sensor =>
                sensor.HardwareType == HardwareType.SuperIO ||
                sensor.HardwareType == HardwareType.Motherboard).ToList());

        AppendFanSection(
            report,
            "Outros sensores de fan",
            fanSensors.Where(sensor =>
                !IsGpuSensor(sensor) &&
                sensor.HardwareType != HardwareType.SuperIO &&
                sensor.HardwareType != HardwareType.Motherboard).ToList());

        report.AppendLine("Controles de fan");
        report.AppendLine("----------------");

        if (fanControls.Count == 0)
        {
            report.AppendLine("Nenhum controle de fan encontrado.");
        }
        else
        {
            foreach (SensorReading control in fanControls)
            {
                report.AppendLine($"- {control.HardwareType} | {control.HardwareName} | {control.SensorName}: {FormatValue(control.Value)} % | ID: {control.SensorIdentifier}");
            }
        }

        report.AppendLine();
        report.AppendLine("Observacoes");
        report.AppendLine("-----------");
        report.AppendLine("- A quantidade de sensores de fan nao representa necessariamente a quantidade fisica de ventoinhas.");
        report.AppendLine("- Placas de video com duas ou mais fans geralmente reportam apenas um sensor chamado GPU Fan.");
        report.AppendLine("- Fans ligadas por hub, splitter, SATA ou Molex podem nao aparecer individualmente.");
        report.AppendLine("- Valor 0 RPM pode indicar fan-stop, fan parada, header vazio ou ausencia de leitura do fio tach.");
        report.AppendLine();
    }

    private static void AppendFanSection(StringBuilder report, string title, List<SensorReading> fans)
    {
        report.AppendLine(title);
        report.AppendLine(new string('-', title.Length));

        if (fans.Count == 0)
        {
            report.AppendLine("Nenhum.");
            report.AppendLine();
            return;
        }

        foreach (SensorReading fan in fans)
        {
            string rpmStatus = fan.Value.HasValue && fan.Value.Value > 0
                ? "girando"
                : "zero/indisponivel";

            report.AppendLine($"- {fan.HardwareType} | {fan.HardwareName} | {fan.SensorName}: {FormatValue(fan.Value)} RPM ({rpmStatus}) | ID: {fan.SensorIdentifier}");
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

    private static void AppendDetectedHardware(
        StringBuilder report,
        string title,
        List<SensorReading> sensors,
        Func<SensorReading, bool> predicate)
    {
        List<SensorReading> hardwareItems = GetDistinctHardware(sensors, predicate)
            .OrderBy(sensor => sensor.HardwareType.ToString())
            .ThenBy(sensor => sensor.HardwareName)
            .ToList();

        report.AppendLine(title);
        report.AppendLine(new string('-', title.Length));

        if (hardwareItems.Count == 0)
        {
            report.AppendLine("Nenhum.");
        }
        else
        {
            foreach (SensorReading hardware in hardwareItems)
            {
                report.AppendLine($"- {hardware.HardwareType}: {hardware.HardwareName}");
            }
        }

        report.AppendLine();
    }

    public static int CountDistinctHardware(List<SensorReading> sensors, Func<SensorReading, bool> predicate)
    {
        return GetDistinctHardware(sensors, predicate).Count;
    }

    private static List<SensorReading> GetDistinctHardware(
        List<SensorReading> sensors,
        Func<SensorReading, bool> predicate)
    {
        return sensors
            .Where(predicate)
            .GroupBy(GetHardwareKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string GetHardwareKey(SensorReading sensor)
    {
        if (!string.IsNullOrWhiteSpace(sensor.HardwareIdentifier))
        {
            return sensor.HardwareIdentifier;
        }

        return $"{sensor.HardwareType}|{sensor.HardwareName}";
    }

    private static bool IsGpuSensor(SensorReading sensor)
    {
        return sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBatterySensor(SensorReading sensor)
    {
        return sensor.HardwareType.ToString().Contains("Battery", StringComparison.OrdinalIgnoreCase);
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
