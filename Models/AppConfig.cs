public class AppConfig
{
    public float CpuTempMax { get; set; } = 80;
    public float GpuTempMax { get; set; } = 80;
    public float SsdTempMax { get; set; } = 60;
    public int IntervaloMs { get; set; } = 2000;
    public bool EnableCsv { get; set; } = true;
    public bool EnableConsole { get; set; } = true;
    public string Mode { get; set; } = "resumo";
    public string CpuFanSensorName { get; set; } = "Fan #2";
    public string TemperatureUnit { get; set; } = "C";
    public bool ShowTemperatureUnitInTrayIcon { get; set; } = false;
    public bool EnableAutoUpdateCheck { get; set; } = true;
}
