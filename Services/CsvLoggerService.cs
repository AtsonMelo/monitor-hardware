using System;
using System.Globalization;
using System.IO;
using System.Text;

class CsvLoggerService
{
    private const string Header = "DataHora,CPU_Temp_C,CPU_Uso_Percent,CPU_Power_W,CPU_Fan_RPM,GPU_Temp_C,GPU_Uso_Percent,GPU_Power_W,GPU_Fan_RPM,SSD_Temp_C,RAM_Uso_Percent,RAM_Usada_GB,RAM_Disponivel_GB,RAM_Total_GB";

    public void Save(MonitorSnapshot snapshot)
    {
        Directory.CreateDirectory("logs");

        string filePath = GetLogFilePath();
        bool novoArquivo = !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

        using StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8);

        if (novoArquivo)
        {
            writer.WriteLine(Header);
        }

        writer.WriteLine(string.Join(",",
            snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            Csv(snapshot.CpuTemp),
            Csv(snapshot.CpuUso),
            Csv(snapshot.CpuPower),
            Csv(snapshot.CpuFan),
            Csv(snapshot.GpuTemp),
            Csv(snapshot.GpuUso),
            Csv(snapshot.GpuPower),
            Csv(snapshot.GpuFan),
            Csv(snapshot.SsdTemp),
            Csv(snapshot.RamUso),
            Csv(snapshot.RamUsadaGb),
            Csv(snapshot.RamDisponivelGb),
            Csv(snapshot.RamTotalGb)
        ));
    }

    private static string Csv(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : "";
    }

    private static string GetLogFilePath()
    {
        string date = DateTime.Now.ToString("yyyyMMdd");
        string filePath = Path.Combine("logs", $"monitor-hardware-{date}.csv");
        int version = 2;

        while (File.Exists(filePath) && !HasCurrentHeader(filePath))
        {
            filePath = Path.Combine("logs", $"monitor-hardware-{date}-v{version}.csv");
            version++;
        }

        return filePath;
    }

    private static bool HasCurrentHeader(string filePath)
    {
        string? firstLine = File.ReadLines(filePath).FirstOrDefault();

        return string.Equals(firstLine, Header, StringComparison.Ordinal);
    }
}
