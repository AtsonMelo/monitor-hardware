using LibreHardwareMonitor.Hardware;

public class SensorReading
{
    public string HardwareName { get; set; } = "";
    public HardwareType HardwareType { get; set; }
    public string HardwareIdentifier { get; set; } = "";

    public string SensorName { get; set; } = "";
    public SensorType SensorType { get; set; }
    public string SensorIdentifier { get; set; } = "";

    public float? Value { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }

    public bool HasValue => Value.HasValue;
}