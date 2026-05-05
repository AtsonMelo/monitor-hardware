using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.Hardware;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Monitor de Hardware - versão resumida com CSV");

        if (args.Contains("--relatorio"))
        {
            try
            {
                HtmlReportService htmlReportService = new HtmlReportService();
                string reportPath = htmlReportService.GenerateLatestReport();

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

            DiagnosticDisplayService diagnosticDisplay = new DiagnosticDisplayService();
            diagnosticDisplay.Show(sensors);

            return;
        }

        Console.WriteLine("Pressione Ctrl + C para sair.");

        ConfigService configService = new ConfigService();
        AppConfig config = configService.Load();

        HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
        ConsoleDisplayService consoleDisplay = new ConsoleDisplayService(config);
        CsvLoggerService csvLogger = new CsvLoggerService();
        SnapshotService snapshotService = new SnapshotService();

        while (true)
        {
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = snapshotService.Create(sensors);

            Console.Clear();
            Console.WriteLine("=== Monitor de Hardware - Resumo ===");
            Console.WriteLine($"Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            consoleDisplay.Show(sensors);

            csvLogger.Save(snapshot);

            Thread.Sleep(config.IntervaloMs);
        }
    }

    static string Csv(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : "";
    }
}

