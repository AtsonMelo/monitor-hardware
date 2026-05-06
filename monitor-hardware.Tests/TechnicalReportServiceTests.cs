using LibreHardwareMonitor.Hardware;

namespace monitor_hardware.Tests;

public class TechnicalReportServiceTests
{
    [Fact]
    public void CountDistinctHardware_WhenSameGpuHasManySensors_CountsOneGpu()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.GpuAmd, "Radeon RX 470", "/gpu-amd/0", SensorType.Temperature, "GPU Core", 56),
            CreateSensor(HardwareType.GpuAmd, "Radeon RX 470", "/gpu-amd/0", SensorType.Load, "GPU Core", 42),
            CreateSensor(HardwareType.GpuAmd, "Radeon RX 470", "/gpu-amd/0", SensorType.Fan, "GPU Fan", 1200)
        };

        int count = TechnicalReportService.CountDistinctHardware(
            sensors,
            sensor => sensor.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, count);
    }

    [Fact]
    public void CountDistinctHardware_WhenTwoStoragesHaveDifferentIdentifiers_CountsTwoStorages()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.Storage, "NVMe A", "/storage/0", SensorType.Temperature, "Composite Temperature", 42),
            CreateSensor(HardwareType.Storage, "NVMe A", "/storage/0", SensorType.Load, "Used Space", 60),
            CreateSensor(HardwareType.Storage, "SSD B", "/storage/1", SensorType.Temperature, "Temperature", 38)
        };

        int count = TechnicalReportService.CountDistinctHardware(
            sensors,
            sensor => sensor.HardwareType == HardwareType.Storage);

        Assert.Equal(2, count);
    }

    [Fact]
    public void CountDistinctHardware_WhenIdentifierIsMissing_UsesTypeAndNameAsFallback()
    {
        List<SensorReading> sensors = new()
        {
            CreateSensor(HardwareType.Network, "Ethernet", "", SensorType.Throughput, "Upload", 10),
            CreateSensor(HardwareType.Network, "Ethernet", "", SensorType.Throughput, "Download", 20),
            CreateSensor(HardwareType.Network, "Wi-Fi", "", SensorType.Throughput, "Download", 30)
        };

        int count = TechnicalReportService.CountDistinctHardware(
            sensors,
            sensor => sensor.HardwareType == HardwareType.Network);

        Assert.Equal(2, count);
    }

    private static SensorReading CreateSensor(
        HardwareType hardwareType,
        string hardwareName,
        string hardwareIdentifier,
        SensorType sensorType,
        string sensorName,
        float? value)
    {
        return new SensorReading
        {
            HardwareType = hardwareType,
            HardwareName = hardwareName,
            HardwareIdentifier = hardwareIdentifier,
            SensorType = sensorType,
            SensorName = sensorName,
            Value = value
        };
    }
}
