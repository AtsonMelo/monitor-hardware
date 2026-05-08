using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


class Program
{
    private const string SingleInstanceMutexName = @"Local\AtsonMelo.MonitorHardware";
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsSingleInstance;

    [STAThread]
    static async Task Main(string[] args)
    {
        try
        {
            await RunAsync(args);
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Falha não tratada na inicialização do app.");
            ErrorReportResult? errorReport = TryCreateStartupErrorReport(ex);

            if (ShouldShowStartupErrorDialog(args))
            {
                string reportText = errorReport == null
                    ? $"Log: {AppLogService.LogPath}"
                    : $"Relatório: {errorReport.ReportPath}\nGitHub: {errorReport.GitHubUrl}";

                DialogResult dialogResult = MessageBox.Show(
                    $"Não foi possível iniciar o Monitor Hardware.\n\n{reportText}\n\nErro: {ex.Message}\n\nDeseja abrir o GitHub para postar o relatório de erros?",
                    "Monitor Hardware",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (dialogResult == DialogResult.Yes)
                {
                    OpenUrl(errorReport?.GitHubUrl ?? ErrorReportService.SupportUrl);
                }
            }
            else
            {
                Console.WriteLine($"Erro fatal: {ex.Message}");
                Console.WriteLine($"Log: {AppLogService.LogPath}");

                if (errorReport != null)
                {
                    Console.WriteLine($"Relatório: {errorReport.ReportPath}");
                    Console.WriteLine($"GitHub: {errorReport.GitHubUrl}");
                }
            }
        }
        finally
        {
            ReleaseSingleInstance();
        }
    }

    private static ErrorReportResult? TryCreateStartupErrorReport(Exception exception)
    {
        try
        {
            ErrorReportResult result = ErrorReportService.Create(exception: exception);

            try
            {
                Clipboard.SetText(result.Content);
            }
            catch (Exception clipboardException)
            {
                AppLogService.Error(clipboardException, "Não foi possível copiar relatório de erro para a área de transferência.");
            }

            return result;
        }
        catch (Exception reportException)
        {
            AppLogService.Error(reportException, "Não foi possível gerar relatório de erro de inicialização.");
            return null;
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível abrir URL de suporte.");
        }
    }

    private static bool ShouldShowStartupErrorDialog(string[] args)
    {
        return args.Contains("--gui") ||
               args.Contains("--tray") ||
               GetConsoleWindow() == IntPtr.Zero;
    }

    private static async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            args = new[] { "--gui" };
        }

        bool isGraphicalMode = args.Contains("--gui") || args.Contains("--tray");

        AppLogService.Info($"App iniciado. Args: {string.Join(" ", args)}");
        if (args.Any(arg => arg.Equals("--version", StringComparison.OrdinalIgnoreCase)))
        {
            AttachParentConsole();
            Console.WriteLine(GetAppVersion());
            return;
        }

        if (isGraphicalMode && !TryAcquireSingleInstance())
        {
            ApplicationConfiguration.Initialize();
            MessageBox.Show(
                "O Monitor Hardware já está em execução. Use o ícone perto do relógio para abrir o painel ou sair.",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (!isGraphicalMode)
        {
            AttachParentConsole();
            Console.WriteLine("Monitor de Hardware");
        }

        if (args.Contains("--gui"))
        {
            ApplicationConfiguration.Initialize();
            ConfigureWindowsFormsErrorHandling();

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

            await RunGuiAsync(guiConfig);

            return;
        }

        if (args.Contains("--tray"))
        {
            ApplicationConfiguration.Initialize();
            ConfigureWindowsFormsErrorHandling();

            ConfigService trayConfigService = new ConfigService();
            AppConfig trayConfig = trayConfigService.Load();

            if (trayConfig.IntervaloMs <= 0)
            {
                Console.WriteLine("IntervaloMs deve ser maior que zero no config.json.");
                return;
            }

            using CancellationTokenSource trayCancellationTokenSource = new CancellationTokenSource();
            using TrayIconService trayIconService = new TrayIconService(trayConfig);

            Application.ApplicationExit += (_, _) => trayCancellationTokenSource.Cancel();

            Task trayMonitorTask = RunTrayMonitorAsync(
                trayConfig,
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
        if (args.Contains("--relatorio-tecnico"))
        {
            try
            {
                ConfigService technicalConfigService = new ConfigService();
                AppConfig technicalConfig = technicalConfigService.Load();

                TechnicalReportResult result = TechnicalReportService.Create(technicalConfig);
                SanitizedReportResult sanitizedResult = ReportSanitizerService.CreateSanitizedCopy(result.ReportPath);

                Console.WriteLine($"Relatório técnico gerado em: {result.ReportPath}");
                Console.WriteLine($"Relatório técnico sanitizado para GitHub gerado em: {sanitizedResult.SanitizedPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Não foi possível gerar o relatório técnico: {ex.Message}");
            }

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

    private static async Task RunGuiAsync(AppConfig config)
    {
        using HardwareDashboardForm dashboardForm = new HardwareDashboardForm(config);
        using CancellationTokenSource trayCancellationTokenSource = new CancellationTokenSource();
        using TrayIconService trayIconService = new TrayIconService(config, dashboardForm);

        Application.ApplicationExit += (_, _) => trayCancellationTokenSource.Cancel();

        Task trayMonitorTask = RunTrayMonitorAsync(
            config,
            trayIconService,
            trayCancellationTokenSource.Token);

        Application.Run(dashboardForm);

        trayIconService.Hide();
        trayCancellationTokenSource.Cancel();

        Task completedTask = await Task.WhenAny(
            trayMonitorTask,
            Task.Delay(TimeSpan.FromSeconds(2)));

        if (completedTask != trayMonitorTask)
        {
            AppLogService.Info("Monitoramento da bandeja não encerrou dentro do tempo limite. Prosseguindo com encerramento do app.");
        }
        else
        {
            await trayMonitorTask;
        }
    }

    private static async Task RunTrayMonitorAsync(
        AppConfig config,
        TrayIconService trayIconService,
        CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.IntervaloMs));
        HardwareMonitorService? hardwareMonitor = null;
        SnapshotService snapshotService = new SnapshotService(config);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                hardwareMonitor ??= new HardwareMonitorService();

                List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
                MonitorSnapshot snapshot = snapshotService.Create(sensors);

                trayIconService.UpdateTooltip(snapshot);
            }
            catch (Exception ex)
            {
                AppLogService.Error(ex, "Não foi possível atualizar o ícone da bandeja.");
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

    private static void ConfigureWindowsFormsErrorHandling()
    {
        Application.ThreadException += (_, eventArgs) =>
        {
            AppLogService.Error(eventArgs.Exception, "Erro não tratado na interface gráfica.");
            MessageBox.Show(
                $"Ocorreu um erro no Monitor Hardware.\n\nLog: {AppLogService.LogPath}\n\nErro: {eventArgs.Exception.Message}",
                "Monitor Hardware",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                AppLogService.Error(exception, "Erro não tratado no processo.");
            }
        };
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

    private static string GetAppVersion()
    {
        string version =
            typeof(Program).Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "desconhecida";

        int metadataIndex = version.IndexOf('+');

        if (metadataIndex > 0)
        {
            version = version[..metadataIndex];
        }

        return version;
    }
    private static void AttachParentConsole()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
    }

    private static bool TryAcquireSingleInstance()
    {
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);

            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }

            _ownsSingleInstance = true;
            return true;
        }
        catch (Exception ex)
        {
            AppLogService.Error(ex, "Não foi possível criar trava de instância única.");
            return true;
        }
    }

    private static void ReleaseSingleInstance()
    {
        if (!_ownsSingleInstance || _singleInstanceMutex == null)
        {
            return;
        }

        try
        {
            _singleInstanceMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }
        finally
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            _ownsSingleInstance = false;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private const int ATTACH_PARENT_PROCESS = -1;
}


