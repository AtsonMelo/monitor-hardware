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
        float? ramUsadaGb = GetMemoryData(sensors, "Memory Used");
        float? ramDisponivelGb = GetMemoryData(sensors, "Memory Available");
        float? ramTotalGb = ramUsadaGb.HasValue && ramDisponivelGb.HasValue
            ? ramUsadaGb.Value + ramDisponivelGb.Value
            : null;

        return new MonitorSnapshot
        {
            Timestamp = DateTime.Now,

            CpuTemp = SensorLookupService.GetCpuTemperature(sensors),
            CpuUso = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Load, "CPU Total"),
            CpuPower = HardwareMonitorService.GetSensor(sensors, HardwareType.Cpu, SensorType.Power, "CPU Package"),
            CpuFan = cpuFan,

            GpuTemp = SensorLookupService.GetGpuTemperature(sensors),
            GpuUso = SensorLookupService.GetGpuLoad(sensors),
            GpuPower = SensorLookupService.GetGpuPower(sensors),
            GpuFan = SensorLookupService.GetGpuFan(sensors),

            SsdTemp = SensorLookupService.GetStorageTemperature(sensors),

            RamUso = sensors
                .FirstOrDefault(s =>
                    s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                    s.SensorType == SensorType.Load &&
                    s.SensorName.Equals("Memory", StringComparison.OrdinalIgnoreCase))
                ?.Value,

            RamUsadaGb = ramUsadaGb,
            RamDisponivelGb = ramDisponivelGb,
            RamTotalGb = ramTotalGb
        };
    }

    private static float? GetMemoryData(List<SensorReading> sensors, string sensorName)
    {
        return sensors
            .FirstOrDefault(s =>
                s.HardwareName.Equals("Total Memory", StringComparison.OrdinalIgnoreCase) &&
                s.SensorType == SensorType.Data &&
                s.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase) &&
                s.Value.HasValue)
            ?.Value;
    }

}
