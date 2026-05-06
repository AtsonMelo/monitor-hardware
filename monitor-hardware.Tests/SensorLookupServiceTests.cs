using LibreHardwareMonitor.Hardware;

namespace monitor_hardware.Tests;

public class SensorLookupServiceTests
{
    [Fact]
    public void GetGpuLoad_WhenGpuIsIntel_ReturnsIntelGpuSensor()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.GpuIntel, SensorType.Load, "GPU Core", 37),
            CreateSensor(HardwareType.GpuIntel, SensorType.Temperature, "GPU Core", 51)
        };

        Assert.Equal(37, SensorLookupService.GetGpuLoad(sensors));
        Assert.Equal(51, SensorLookupService.GetGpuTemperature(sensors));
        Assert.Equal("Intel(R) Graphics", SensorLookupService.GetGpuName(sensors));
    }

    [Fact]
    public void GetStorageTemperature_WhenCompositeTemperatureExists_ReturnsCompositeTemperature()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Warning Temperature", 85),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Critical Temperature", 86),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Temperature #1", 56.9f),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Composite Temperature", 48)
        };

        Assert.Equal(48, SensorLookupService.GetStorageTemperature(sensors));
    }

    [Fact]
    public void GetStorageTemperature_WhenOnlyNumberedTemperaturesExist_IgnoresWarningAndCritical()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Warning Temperature", 85),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Critical Temperature", 86),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Temperature #1", 56.9f),
            CreateSensor(HardwareType.Storage, SensorType.Temperature, "Temperature #2", 47.9f)
        };

        Assert.Equal(56.9f, SensorLookupService.GetStorageTemperature(sensors));
    }

    [Fact]
    public void GetCpuTemperature_WhenPreferredSensorsHaveNoValue_ReturnsHighestValidCpuTemperature()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.Cpu, SensorType.Temperature, "CPU Package", null),
            CreateSensor(HardwareType.Cpu, SensorType.Temperature, "Distance to TjMax", 70),
            CreateSensor(HardwareType.Cpu, SensorType.Temperature, "P-Core #1", 44),
            CreateSensor(HardwareType.Cpu, SensorType.Temperature, "E-Core #1", 41)
        };

        Assert.Equal(44, SensorLookupService.GetCpuTemperature(sensors));
    }

    private static SensorReading CreateSensor(
        HardwareType hardwareType,
        SensorType sensorType,
        string sensorName,
        float? value)
    {
        return new SensorReading
        {
            HardwareName = hardwareType == HardwareType.GpuIntel
                ? "Intel(R) Graphics"
                : hardwareType.ToString(),
            HardwareType = hardwareType,
            SensorName = sensorName,
            SensorType = sensorType,
            Value = value
        };
    }
}
