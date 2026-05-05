using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

class ConsoleDisplayService
{
    public void Show(List<SensorReading> sensors)
    {
        MostrarCpu(sensors);
        MostrarGpu(sensors);
        MostrarSsd(sensors);
        MostrarMemoria(sensors);
        MostrarRede(sensors);
        MostrarAlertas(sensors);
    }

    private static string F(float? value, string unit = "")
    {
        return value == null ? "--" : $"{value:F1}{unit}";
    }

    private static void MostrarCpu(List<SensorReading> sensors)
    {
        float? cpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "CPU Package");
        float? cpuCoreMax = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "Core Max");
        float? cpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Load, "CPU Total");
        float? cpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Power, "CPU Package");
        float? cpuClock = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Clock, "CPU Core #1");

        float? cpuFan = sensors
            .Where(s => s.SensorType == SensorType.Fan && s.Value != null && s.Value > 0)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault()
            ?.Value;

        Console.WriteLine("CPU");
        Console.WriteLine($"  Temperatura Package : {F(cpuTemp, " °C")}");
        Console.WriteLine($"  Temperatura Core Max: {F(cpuCoreMax, " °C")}");
        Console.WriteLine($"  Uso total           : {F(cpuUso, " %")}");
        Console.WriteLine($"  Potência Package    : {F(cpuPower, " W")}");
        Console.WriteLine($"  Clock Core #1       : {F(cpuClock, " MHz")}");
        Console.WriteLine($"  Fan provável CPU    : {F(cpuFan, " RPM")}");
        Console.WriteLine();
    }

    private static void MostrarGpu(List<SensorReading> sensors)
    {
        float? gpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Temperature, "GPU Core");
        float? gpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Load, "GPU Core");
        float? gpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Power, "GPU Package");
        float? gpuClock = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Clock, "GPU Core");
        float? gpuMemClock = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Clock, "GPU Memory");
        float? gpuFan = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Fan, "GPU Fan");

        Console.WriteLine("GPU - Radeon RX 470");
        Console.WriteLine($"  Temperatura : {F(gpuTemp, " °C")}");
        Console.WriteLine($"  Uso         : {F(gpuUso, " %")}");
        Console.WriteLine($"  Potência    : {F(gpuPower, " W")}");
        Console.WriteLine($"  Clock Core  : {F(gpuClock, " MHz")}");
        Console.WriteLine($"  Clock Mem   : {F(gpuMemClock, " MHz")}");
        Console.WriteLine($"  Fan         : {F(gpuFan, " RPM")}");
        Console.WriteLine();
    }

    private static void MostrarSsd(List<SensorReading> sensors)
    {
        float? ssdTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Temperature, "Temperature");
        float? ssdVida = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Level, "Life");
        float? ssdUso = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Load, "Used Space");
        float? ssdAtividade = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Load, "Total Activity");

        Console.WriteLine("SSD");
        Console.WriteLine($"  Temperatura : {F(ssdTemp, " °C")}");
        Console.WriteLine($"  Vida útil   : {F(ssdVida, " %")}");
        Console.WriteLine($"  Espaço usado: {F(ssdUso, " %")}");
        Console.WriteLine($"  Atividade   : {F(ssdAtividade, " %")}");
        Console.WriteLine();
    }

    private static void MostrarMemoria(List<SensorReading> sensors)
    {
        float? ramUso = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Load &&
                s.SensorName.Equals("Memory", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        float? ramUsada = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Data &&
                s.SensorName.Equals("Memory Used", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        float? ramLivre = sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Data &&
                s.SensorName.Equals("Memory Available", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        Console.WriteLine("Memória RAM");
        Console.WriteLine($"  Uso       : {F(ramUso, " %")}");
        Console.WriteLine($"  Usada     : {F(ramUsada, " GB")}");
        Console.WriteLine($"  Disponível: {F(ramLivre, " GB")}");
        Console.WriteLine();
    }

    private static void MostrarRede(List<SensorReading> sensors)
    {
        float? down = HardwareMonitorService.GetFirstSensor(sensors, SensorType.Throughput, "Download Speed");
        float? up = HardwareMonitorService.GetFirstSensor(sensors, SensorType.Throughput, "Upload Speed");

        Console.WriteLine("Rede");
        Console.WriteLine($"  Download: {F(down)} B/s");
        Console.WriteLine($"  Upload  : {F(up)} B/s");
        Console.WriteLine();
    }

    private static void MostrarAlertas(List<SensorReading> sensors)
    {
        float? cpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "CPU Package");
        float? gpuTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.GpuAmd, SensorType.Temperature, "GPU Core");
        float? ssdTemp = HardwareMonitorService.GetSensor(sensors, HardwareType.Storage, SensorType.Temperature, "Temperature");

        Console.WriteLine("Alertas");

        bool alerta = false;

        if (cpuTemp >= 80)
        {
            Console.WriteLine($"  ALERTA: CPU alta: {cpuTemp:F1} °C");
            alerta = true;
        }

        if (gpuTemp >= 80)
        {
            Console.WriteLine($"  ALERTA: GPU alta: {gpuTemp:F1} °C");
            alerta = true;
        }

        if (ssdTemp >= 60)
        {
            Console.WriteLine($"  ALERTA: SSD quente: {ssdTemp:F1} °C");
            alerta = true;
        }

        if (!alerta)
        {
            Console.WriteLine("  Nenhum alerta crítico.");
        }

        Console.WriteLine();
    }
}