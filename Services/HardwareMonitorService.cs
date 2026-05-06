using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            IsNetworkEnabled = true,
            IsBatteryEnabled = true
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

    public List<SensorReading> ReadAllSensorsWithWarmup(int attempts = 3, int delayMs = 350)
    {
        if (attempts < 1)
        {
            attempts = 1;
        }

        Dictionary<string, SensorReading> bestReadings = new();

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            List<SensorReading> currentReadings = ReadAllSensors();

            foreach (SensorReading reading in currentReadings)
            {
                string key = GetSensorKey(reading);

                if (!bestReadings.TryGetValue(key, out SensorReading? existing))
                {
                    bestReadings[key] = reading;
                    continue;
                }

                if (reading.Value.HasValue || !existing.Value.HasValue)
                {
                    bestReadings[key] = reading;
                }
            }

            if (attempt < attempts - 1)
            {
                Thread.Sleep(delayMs);
            }
        }

        return bestReadings
            .Values
            .OrderBy(sensor => sensor.HardwareType.ToString())
            .ThenBy(sensor => sensor.HardwareName)
            .ThenBy(sensor => sensor.SensorType.ToString())
            .ThenBy(sensor => sensor.SensorName)
            .ToList();
    }

    private static string GetSensorKey(SensorReading reading)
    {
        if (!string.IsNullOrWhiteSpace(reading.SensorIdentifier))
        {
            return reading.SensorIdentifier;
        }

        return $"{reading.HardwareType}|{reading.HardwareName}|{reading.SensorType}|{reading.SensorName}";
    }

    private void ReadHardwareRecursive(IHardware hardware, List<SensorReading> sensors)
    {
        hardware.Update();

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Update();
        }

        foreach (ISensor sensor in hardware.Sensors)
        {
            sensors.Add(new SensorReading
            {
                HardwareName = hardware.Name,
                HardwareType = hardware.HardwareType,
                HardwareIdentifier = hardware.Identifier.ToString(),

                SensorName = sensor.Name,
                SensorType = sensor.SensorType,
                SensorIdentifier = sensor.Identifier.ToString(),

                Value = sensor.Value,
                Min = sensor.Min,
                Max = sensor.Max
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