using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

class ConsoleDisplayService
{
    private readonly AppConfig _config;

    public ConsoleDisplayService(AppConfig config)
    {
        _config = config;
    }

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

    private void MostrarCpu(List<SensorReading> sensors)
    {
        float? cpuTemp = SensorLookupService.GetCpuTemperature(sensors);
        float? cpuCoreMax = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Temperature, "Core Max");
        float? cpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Load, "CPU Total");
        float? cpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Power, "CPU Package");
        float? cpuClock = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Clock, "CPU Core #1");
        float? cpuFan = HardwareMonitorService.GetCpuFan(sensors, _config.CpuFanSensorName);

        Console.WriteLine("CPU");
        Console.WriteLine($"  Temperatura         : {F(cpuTemp, " °C")}");
        Console.WriteLine($"  Temperatura Core Max: {F(cpuCoreMax, " °C")}");
        Console.WriteLine($"  Uso total           : {F(cpuUso, " %")}");
        Console.WriteLine($"  Potência Package    : {F(cpuPower, " W")}");
        Console.WriteLine($"  Clock Core #1       : {F(cpuClock, " MHz")}");
        Console.WriteLine(cpuFan.HasValue
            ? $"  Fan CPU             : {F(cpuFan, " RPM")}"
            : "  Fan CPU             : não disponível neste hardware");
        Console.WriteLine();
    }

    private static void MostrarGpu(List<SensorReading> sensors)
    {
        float? gpuTemp = SensorLookupService.GetGpuTemperature(sensors);
        float? gpuUso = SensorLookupService.GetGpuLoad(sensors);
        float? gpuPower = SensorLookupService.GetGpuPower(sensors);
        float? gpuClock = SensorLookupService.GetGpuCoreClock(sensors);
        float? gpuMemClock = SensorLookupService.GetGpuMemoryClock(sensors);
        float? gpuFan = SensorLookupService.GetGpuFan(sensors);

        Console.WriteLine($"GPU - {SensorLookupService.GetGpuName(sensors)}");
        Console.WriteLine($"  Temperatura : {F(gpuTemp, " °C")}");
        Console.WriteLine($"  Uso         : {F(gpuUso, " %")}");
        Console.WriteLine($"  Potência    : {F(gpuPower, " W")}");
        Console.WriteLine($"  Clock Core  : {F(gpuClock, " MHz")}");
        Console.WriteLine($"  Clock Mem   : {F(gpuMemClock, " MHz")}");
        Console.WriteLine(gpuFan.HasValue
            ? $"  Fan         : {F(gpuFan, " RPM")}"
            : "  Fan         : não disponível neste hardware");
        Console.WriteLine();
    }

    private static void MostrarSsd(List<SensorReading> sensors)
    {
        float? ssdTemp = SensorLookupService.GetStorageTemperature(sensors);
        float? ssdVida = SensorLookupService.GetStorageLife(sensors);
        float? ssdUso = SensorLookupService.GetStorageUsedSpace(sensors);
        float? ssdAtividade = SensorLookupService.GetStorageActivity(sensors);

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

    private void MostrarAlertas(List<SensorReading> sensors)
    {
        float? cpuTemp = SensorLookupService.GetCpuTemperature(sensors);
        float? gpuTemp = SensorLookupService.GetGpuTemperature(sensors);
        float? ssdTemp = SensorLookupService.GetStorageTemperature(sensors);

        Console.WriteLine("Alertas");

        bool alerta = false;

        if (cpuTemp >= _config.CpuTempMax)
        {
            Console.WriteLine($"  ALERTA: CPU alta: {cpuTemp:F1} °C");
            alerta = true;
        }

        if (gpuTemp >= _config.GpuTempMax)
        {
            Console.WriteLine($"  ALERTA: GPU alta: {gpuTemp:F1} °C");
            alerta = true;
        }

        if (ssdTemp >= _config.SsdTempMax)
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
