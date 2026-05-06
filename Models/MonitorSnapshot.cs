using System;

class MonitorSnapshot
{
    public DateTime Timestamp { get; set; }

    public float? CpuTemp { get; set; }
    public float? CpuUso { get; set; }
    public float? CpuPower { get; set; }
    public float? CpuFan { get; set; }

    public float? GpuTemp { get; set; }
    public float? GpuUso { get; set; }
    public float? GpuPower { get; set; }
    public float? GpuFan { get; set; }

    public float? SsdTemp { get; set; }
    public float? RamUso { get; set; }
    public float? RamUsadaGb { get; set; }
    public float? RamDisponivelGb { get; set; }
    public float? RamTotalGb { get; set; }
}
