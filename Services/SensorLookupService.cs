using LibreHardwareMonitor.Hardware;

public static class SensorLookupService
{
    public static bool IsGpuHardware(HardwareType hardwareType)
    {
        return hardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetGpuName(List<SensorReading> sensors)
    {
        return sensors
                   .FirstOrDefault(sensor => IsGpuHardware(sensor.HardwareType))
                   ?.HardwareName
               ?? "GPU";
    }

    public static float? GetCpuTemperature(List<SensorReading> sensors)
    {
        string[] preferredNames =
        {
            "CPU Package",
            "Core Max",
            "Core Average"
        };

        foreach (string sensorName in preferredNames)
        {
            float? value = GetExactSensor(
                sensors,
                sensor => sensor.HardwareType == HardwareType.Cpu,
                SensorType.Temperature,
                sensorName);

            if (value.HasValue)
            {
                return value;
            }
        }

        return sensors
            .Where(sensor =>
                sensor.HardwareType == HardwareType.Cpu &&
                sensor.SensorType == SensorType.Temperature &&
                sensor.Value.HasValue &&
                !sensor.SensorName.Contains("Distance to TjMax", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault()
            ?.Value;
    }

    public static float? GetGpuTemperature(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Temperature,
            "GPU Core",
            "GPU Temperature",
            "Core",
            "Temperature");
    }

    public static float? GetGpuLoad(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Load,
            "GPU Core",
            "GPU Load",
            "D3D 3D",
            "3D",
            "Video Engine");
    }

    public static float? GetGpuPower(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Power,
            "GPU Package",
            "GPU Power",
            "Package",
            "Power");
    }

    public static float? GetGpuFan(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Fan,
            "GPU Fan",
            "Fan");
    }

    public static float? GetGpuCoreClock(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Clock,
            "GPU Core",
            "Core",
            "Graphics");
    }

    public static float? GetGpuMemoryClock(List<SensorReading> sensors)
    {
        return GetPreferredGpuSensor(
            sensors,
            SensorType.Clock,
            "GPU Memory",
            "Memory");
    }

    public static float? GetStorageTemperature(List<SensorReading> sensors)
    {
        List<SensorReading> storageTemperatures = sensors
            .Where(sensor =>
                sensor.HardwareType == HardwareType.Storage &&
                sensor.SensorType == SensorType.Temperature &&
                sensor.Value.HasValue &&
                IsCurrentStorageTemperature(sensor.SensorName))
            .ToList();

        string[] preferredNames =
        {
            "Composite Temperature",
            "Temperature",
            "Temperature #1",
            "Temperature #2"
        };

        foreach (string sensorName in preferredNames)
        {
            float? exactValue = storageTemperatures
                .FirstOrDefault(sensor => sensor.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (exactValue.HasValue)
            {
                return exactValue;
            }
        }

        return storageTemperatures
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault()
            ?.Value;
    }

    public static float? GetStorageLife(List<SensorReading> sensors)
    {
        return GetPreferredStorageSensor(
            sensors,
            SensorType.Level,
            "Life",
            "Remaining Life",
            "Available Spare");
    }

    public static float? GetStorageUsedSpace(List<SensorReading> sensors)
    {
        return GetPreferredStorageSensor(
            sensors,
            SensorType.Load,
            "Used Space",
            "Space");
    }

    public static float? GetStorageActivity(List<SensorReading> sensors)
    {
        return GetPreferredStorageSensor(
            sensors,
            SensorType.Load,
            "Total Activity",
            "Activity");
    }

    private static float? GetPreferredGpuSensor(
        List<SensorReading> sensors,
        SensorType sensorType,
        params string[] preferredNames)
    {
        return GetPreferredSensor(
            sensors,
            sensor => IsGpuHardware(sensor.HardwareType),
            sensorType,
            preferredNames);
    }

    private static float? GetPreferredStorageSensor(
        List<SensorReading> sensors,
        SensorType sensorType,
        params string[] preferredNames)
    {
        return GetPreferredSensor(
            sensors,
            sensor => sensor.HardwareType == HardwareType.Storage,
            sensorType,
            preferredNames);
    }

    private static float? GetPreferredSensor(
        List<SensorReading> sensors,
        Func<SensorReading, bool> hardwarePredicate,
        SensorType sensorType,
        params string[] preferredNames)
    {
        List<SensorReading> candidates = sensors
            .Where(sensor =>
                hardwarePredicate(sensor) &&
                sensor.SensorType == sensorType &&
                sensor.Value.HasValue)
            .ToList();

        foreach (string preferredName in preferredNames)
        {
            float? exactValue = candidates
                .FirstOrDefault(sensor => sensor.SensorName.Equals(preferredName, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (exactValue.HasValue)
            {
                return exactValue;
            }
        }

        foreach (string preferredName in preferredNames)
        {
            float? containsValue = candidates
                .FirstOrDefault(sensor => sensor.SensorName.Contains(preferredName, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (containsValue.HasValue)
            {
                return containsValue;
            }
        }

        return candidates.FirstOrDefault()?.Value;
    }

    private static float? GetExactSensor(
        List<SensorReading> sensors,
        Func<SensorReading, bool> hardwarePredicate,
        SensorType sensorType,
        string sensorName)
    {
        return sensors
            .FirstOrDefault(sensor =>
                hardwarePredicate(sensor) &&
                sensor.SensorType == sensorType &&
                sensor.SensorName.Equals(sensorName, StringComparison.OrdinalIgnoreCase) &&
                sensor.Value.HasValue)
            ?.Value;
    }

    private static bool IsCurrentStorageTemperature(string sensorName)
    {
        string[] excludedParts =
        {
            "Warning",
            "Critical",
            "Limit",
            "Threshold"
        };

        return !excludedParts.Any(part => sensorName.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}
