using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Monitor de Hardware");

        if (args.Contains("--relatorio"))
        {
            try
            {
                HtmlReportService htmlReportService = new HtmlReportService();
                string reportPath = htmlReportService.GenerateHistoricalReport();

                Console.WriteLine($"Relatório HTML gerado em: {reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Não foi possível gerar o relatório: {ex.Message}");
            }

            return;
        }

        if (args.Contains("--diagnostico"))
        {
            HardwareMonitorService diagnosticHardwareMonitor = new HardwareMonitorService();
            List<SensorReading> sensors = diagnosticHardwareMonitor.ReadAllSensors();

            DiagnosticDisplayService diagnosticDisplayOnce = new DiagnosticDisplayService();
            diagnosticDisplayOnce.Show(sensors);

            return;
        }

        ConfigService configService = new ConfigService();
        AppConfig config = configService.Load();
        string mode = GetMode(args, config);

        if (!IsSupportedMode(mode))
        {
            Console.WriteLine(string.IsNullOrWhiteSpace(mode)
                ? "Modo não informado."
                : $"Modo '{mode}' não é suportado.");
            Console.WriteLine("Use: resumo, detalhado ou somente-log.");
            return;
        }

        bool enableCsv = ShouldEnableCsv(mode, config);
        bool enableConsole = ShouldEnableConsole(mode, config);

        if (!enableConsole && !enableCsv)
        {
            Console.WriteLine("EnableConsole e EnableCsv estão desativados. Ative pelo menos uma saída no config.json.");
            return;
        }

        Console.WriteLine($"Modo de execução: {mode}");
        Console.WriteLine(enableConsole
            ? "Pressione Ctrl + C para sair."
            : "Console desativado; gravando CSV em logs/. Pressione Ctrl + C para sair.");

        HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
        ConsoleDisplayService consoleDisplay = new ConsoleDisplayService(config);
        DiagnosticDisplayService detailedDisplay = new DiagnosticDisplayService();
        CsvLoggerService csvLogger = new CsvLoggerService();
        SnapshotService snapshotService = new SnapshotService(config);

        while (true)
        {
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = snapshotService.Create(sensors);

            if (enableConsole)
            {
                Console.Clear();
                Console.WriteLine(GetTitle(mode));
                Console.WriteLine($"Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();

                if (IsDetailedMode(mode))
                {
                    detailedDisplay.Show(sensors, "Sensores reais detectados");
                }
                else
                {
                    consoleDisplay.Show(sensors);
                }
            }

            if (enableCsv)
            {
                csvLogger.Save(snapshot);
            }

            Thread.Sleep(config.IntervaloMs);
        }
    }

    private static string GetMode(string[] args, AppConfig config)
    {
        string? modeFromEquals = args
            .FirstOrDefault(arg => arg.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=', 2)[1];

        if (!string.IsNullOrWhiteSpace(modeFromEquals))
        {
            return NormalizeMode(modeFromEquals);
        }

        int modeIndex = Array.FindIndex(args, arg => arg.Equals("--mode", StringComparison.OrdinalIgnoreCase));

        if (modeIndex >= 0 && modeIndex + 1 < args.Length)
        {
            return NormalizeMode(args[modeIndex + 1]);
        }

        if (modeIndex >= 0)
        {
            return "";
        }

        return NormalizeMode(config.Mode);
    }

    private static string NormalizeMode(string mode)
    {
        string normalized = mode.Trim().ToLowerInvariant();

        return normalized == "log"
            ? "somente-log"
            : normalized;
    }

    private static bool IsSupportedMode(string mode)
    {
        return mode is "resumo" or "detalhado" or "somente-log";
    }

    private static bool IsDetailedMode(string mode)
    {
        return mode.Equals("detalhado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldEnableCsv(string mode, AppConfig config)
    {
        return mode.Equals("somente-log", StringComparison.OrdinalIgnoreCase) || config.EnableCsv;
    }

    private static bool ShouldEnableConsole(string mode, AppConfig config)
    {
        return !mode.Equals("somente-log", StringComparison.OrdinalIgnoreCase) && config.EnableConsole;
    }

    private static string GetTitle(string mode)
    {
        return IsDetailedMode(mode)
            ? "=== Monitor de Hardware - Detalhado ==="
            : "=== Monitor de Hardware - Resumo ===";
    }
}

