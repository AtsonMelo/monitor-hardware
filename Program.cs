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
    static void Main()
    {
        Console.WriteLine("Monitor de Hardware - versão resumida com CSV");
        Console.WriteLine("Pressione Ctrl + C para sair.");

        ConfigService configService = new ConfigService();
        AppConfig config = configService.Load();

        HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
        ConsoleDisplayService consoleDisplay = new ConsoleDisplayService(config);
        CsvLoggerService csvLogger = new CsvLoggerService();
 
        while (true)
        {
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = CriarSnapshot(sensors);

            Console.Clear();
            Console.WriteLine("=== Monitor de Hardware - Resumo ===");
            Console.WriteLine($"Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            consoleDisplay.Show(sensors);

            csvLogger.Save(snapshot);

            Thread.Sleep(config.IntervaloMs);
        }
    }

    static MonitorSnapshot CriarSnapshot(List<SensorReading> sensors)
    {
        float? cpuFan = sensors
            .Where(s => s.SensorType == SensorType.Fan && s.Value != null && s.Value > 0)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault()
            ?.Value;

        return new MonitorSnapshot
        {
            Timestamp = DateTime.Now,

            CpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "CPU Package"),
            CpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Load, "CPU Total"),
            CpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Power, "CPU Package"),
            CpuFan = cpuFan,

            GpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Temperature, "GPU Core"),
            GpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Load, "GPU Core"),
            GpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Power, "GPU Package"),

            SsdTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Temperature, "Temperature"),

            RamUso = sensors
                .FirstOrDefault(s =>
                    s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                    s.SensorType == SensorType.Load &&
                    s.SensorName.Equals("Memory", StringComparison.OrdinalIgnoreCase))
                ?.Value
        };
    }

    static string Csv(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : "";
    }
}

