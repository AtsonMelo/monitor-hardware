using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

public class HardwareMonitorService
{
    private readonly Computer _computer;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsMemoryEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true
        };

        _computer.Open();
    }

    public List<SensorReading> ReadAllSensors()
    {
        List<SensorReading> sensors = new();

        foreach (IHardware hardware in _computer.Hardware)
        {
            ReadHardwareRecursive(hardware, sensors);
        }

        return sensors;
    }

    private void ReadHardwareRecursive(IHardware hardware, List<SensorReading> sensors)
    {
        hardware.Update();

        foreach (ISensor sensor in hardware.Sensors)
        {
            sensors.Add(new SensorReading
            {
                HardwareName = hardware.Name,
                HardwareType = hardware.HardwareType,
                SensorName = sensor.Name,
                SensorType = sensor.SensorType,
                Value = sensor.Value
            });
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            ReadHardwareRecursive(subHardware, sensors);
        }
    }

    public static float? GetSensor(
        List<SensorReading> sensors,
        HardwareType hardwareType,
        SensorType sensorType,
        string sensorName)
    {
        return sensors
            .FirstOrDefault(s =>
                s.HardwareType == hardwareType &&
                s.SensorType == sensorType &&
                s.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase) &&
                s.Value != null)
            ?.Value;
    }

    public static float? GetFirstSensor(
        List<SensorReading> sensors,
        SensorType sensorType,
        string sensorNameContains)
    {
        return sensors
            .FirstOrDefault(s =>
                s.SensorType == sensorType &&
                s.SensorName.Contains(sensorNameContains, StringComparison.OrdinalIgnoreCase) &&
                s.Value != null)
            ?.Value;
    }

    public static float? GetCpuFan(List<SensorReading> sensors, string sensorName)
    {
        if (!string.IsNullOrWhiteSpace(sensorName))
        {
            float? configuredFan = sensors
                .FirstOrDefault(s =>
                    s.SensorType == SensorType.Fan &&
                    s.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase) &&
                    s.Value != null)
                ?.Value;

            if (configuredFan != null)
            {
                return configuredFan;
            }
        }

        return sensors
            .Where(s => s.SensorType == SensorType.Fan && s.Value != null && s.Value > 0)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault()
            ?.Value;
    }
}
