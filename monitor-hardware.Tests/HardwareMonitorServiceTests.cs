using LibreHardwareMonitor.Hardware;

namespace monitor_hardware.Tests;

public class HardwareMonitorServiceTests
{
    [Fact]
    public void GetCpuFan_WhenConfiguredFanExists_ReturnsConfiguredFan()
    {
        List<SensorReading> sensors = new()
        {
            CreateFan("Fan #1", 2400),
            CreateFan("Fan #2", 1500)
        };

        float? cpuFan = HardwareMonitorService.GetCpuFan(sensors, "Fan #2");

        Assert.Equal(1500, cpuFan);
    }

    [Fact]
    public void GetCpuFan_WhenConfiguredFanDoesNotExist_ReturnsHighestAvailableFan()
    {
        List<SensorReading> sensors = new()
        {
            CreateFan("Fan #1", 900),
            CreateFan("Fan #3", 1800)
        };

        float? cpuFan = HardwareMonitorService.GetCpuFan(sensors, "Fan #2");

        Assert.Equal(1800, cpuFan);
    }

    private static SensorReading CreateFan(string sensorName, float value)
    {
        return new SensorReading
        {
            HardwareName = "ITE IT8613E",
            HardwareType = HardwareType.SuperIO,
            SensorName = sensorName,
            SensorType = SensorType.Fan,
            Value = value
        };
    }
}
