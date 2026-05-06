using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

class SnapshotService
{
    private readonly AppConfig _config;

    public SnapshotService(AppConfig config)
    {
        _config = config;
    }

    public MonitorSnapshot Create(List<SensorReading> sensors)
    {
        float? cpuFan = HardwareMonitorService.GetCpuFan(sensors, _config.CpuFanSensorName);

        return new MonitorSnapshot
        {
            Timestamp = DateTime.Now,

            CpuTemp = GetCpuTemperature(sensors),
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

    private static float? GetCpuTemperature(List<SensorReading> sensors)
    {
        string[] preferredSensorNames =
        {
            "CPU Package",
            "Core Max",
            "Core Average"
        };

        foreach (string sensorName in preferredSensorNames)
        {
            float? value = HardwareMonitorService.GetSensor(
                sensors,
                HardwareType.Cpu,
                SensorType.Temperature,
                sensorName);

            if (value.HasValue)
            {
                return value;
            }
        }

        return sensors
            .Where(s =>
                s.HardwareType == HardwareType.Cpu &&
                s.SensorType == SensorType.Temperature &&
                s.Value.HasValue &&
                !s.SensorName.Contains("Distance to TjMax", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Value)
            .FirstOrDefault()
            ?.Value;
    }
}
