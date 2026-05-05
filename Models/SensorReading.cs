using LibreHardwareMonitor.Hardware;

public class SensorReading
{
    public string HardwareName { get; set; } = "";
    public HardwareType HardwareType { get; set; }
    public string SensorName { get; set; } = "";
    public SensorType SensorType { get; set; }
    public float? Value { get; set; }
}
