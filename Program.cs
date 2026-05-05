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

        HardwareMonitorService hardwareMonitor = new HardwareMonitorService();
        ConsoleDisplayService consoleDisplay = new ConsoleDisplayService();

        while (true)
        {
            List<SensorReading> sensors = hardwareMonitor.ReadAllSensors();
            MonitorSnapshot snapshot = CriarSnapshot(sensors);

            Console.Clear();
            Console.WriteLine("=== Monitor de Hardware - Resumo ===");
            Console.WriteLine($"Atualizado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();
            consoleDisplay.Show(sensors);

            GravarCsv(snapshot);

            Thread.Sleep(2000);
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

    static void GravarCsv(MonitorSnapshot snapshot)
    {
        Directory.CreateDirectory("logs");

        string filePath = Path.Combine("logs", $"monitor-hardware-{DateTime.Now:yyyyMMdd}.csv");
        bool novoArquivo = !File.Exists(filePath);

        using StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8);

        if (novoArquivo)
        {
            writer.WriteLine("DataHora,CPU_Temp_C,CPU_Uso_Percent,CPU_Power_W,CPU_Fan_RPM,GPU_Temp_C,GPU_Uso_Percent,GPU_Power_W,SSD_Temp_C,RAM_Uso_Percent");
        }

        writer.WriteLine(string.Join(",",
            snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            Csv(snapshot.CpuTemp),
            Csv(snapshot.CpuUso),
            Csv(snapshot.CpuPower),
            Csv(snapshot.CpuFan),
            Csv(snapshot.GpuTemp),
            Csv(snapshot.GpuUso),
            Csv(snapshot.GpuPower),
            Csv(snapshot.SsdTemp),
            Csv(snapshot.RamUso)
        ));
    }

    static string Csv(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : "";
    }
}


