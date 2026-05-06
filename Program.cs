using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        Console.WriteLine("Monitor de Hardware");
        if (args.Contains("--gui"))
        {
            ApplicationConfiguration.Initialize();

            ConfigService guiConfigService = new ConfigService();
            AppConfig guiConfig = guiConfigService.Load();

            if (guiConfig.IntervaloMs <= 0)
            {
                MessageBox.Show(
                    "IntervaloMs deve ser maior que zero no config.json.",
                    "Monitor Hardware",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Application.Run(new HardwareDashboardForm(guiConfig));

            return;
        }

        if (args.Contains("--tray"))
        {
            ApplicationConfiguration.Initialize();

            ConfigService trayConfigService = new ConfigService();
            AppConfig trayConfig = trayConfigService.Load();

            if (trayConfig.IntervaloMs <= 0)
            {
                Console.WriteLine("IntervaloMs deve ser maior que zero no config.json.");
                return;
            }

            HardwareMonitorService trayHardwareMonitor = new HardwareMonitorService();
            SnapshotService traySnapshotService = new SnapshotService(trayConfig);

            using CancellationTokenSource trayCancellationTokenSource = new CancellationTokenSource();
            using TrayIconService trayIconService = new TrayIconService(trayConfig);

            Application.ApplicationExit += (_, _) => trayCancellationTokenSource.Cancel();

            Task trayMonitorTask = RunTrayMonitorAsync(
                trayConfig,
                trayHardwareMonitor,
                traySnapshotService,
                trayIconService,
                trayCancellationTokenSource.Token);

            Console.WriteLine("Ícone iniciado na bandeja. Use o menu do ícone para sair.");

            Application.Run();

            await trayMonitorTask;

            return;
        }



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

        if (config.IntervaloMs <= 0)
        {
            Console.WriteLine("IntervaloMs deve ser maior que zero no config.json.");
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

        using CancellationTokenSource cancellationTokenSource = CreateCancellationTokenSource();

        await RunMonitorAsync(
            config,
            mode,
            enableConsole,
            enableCsv,
            hardwareMonitor,
            consoleDisplay,
            detailedDisplay,
            csvLogger,
            snapshotService,
            cancellationTokenSource.Token);
    }

    private static async Task RunTrayMonitorAsync(
        AppConfig config,
        HardwareMonitorService hardwareMonitor,
        SnapshotService snapshotService,
        TrayIconService trayIconService,
        CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.IntervaloMs));

        while (!cancellationToken.IsCancellationRequested)
        {
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = snapshotService.Create(sensors);

            trayIconService.UpdateTooltip(snapshot);

            try
            {
                bool shouldContinue = await timer.WaitForNextTickAsync(cancellationToken);

                if (!shouldContinue)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    private static async Task RunMonitorAsync(
        AppConfig config,
        string mode,
        bool enableConsole,
        bool enableCsv,
        HardwareMonitorService hardwareMonitor,
        ConsoleDisplayService consoleDisplay,
        DiagnosticDisplayService detailedDisplay,
        CsvLoggerService csvLogger,
        SnapshotService snapshotService,
        CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.IntervaloMs));

        while (!cancellationToken.IsCancellationRequested)
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

            try
            {
                bool shouldContinue = await timer.WaitForNextTickAsync(cancellationToken);

                if (!shouldContinue)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine();
        Console.WriteLine("Monitor encerrado.");
    }

    private static CancellationTokenSource CreateCancellationTokenSource()
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        return cancellationTokenSource;
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

