using System;
using System.Globalization;
using System.IO;
using System.Text;

class CsvLoggerService
{
    public void Save(MonitorSnapshot snapshot)
    {
        Directory.CreateDirectory("logs");

        string filePath = Path.Combine("logs", $"monitor-hardware-{DateTime.Now:yyyyMMdd}.csv");
        bool novoArquivo = !File.Exists(filePath);

        using StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8);

        if (novoArquivo)
        {
            writer.WriteLine("DataHora,CPU_Temp_C,CPU_Uso_Percent,CPU_Power_W,CPU_Fan_RPM,GPU_Temp_C,GPU_Uso_Percent,GPU_Power_W,SSD_Temp_C,RAM_Uso_Percent");
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
            Csv(snapshot.SsdTemp),
            Csv(snapshot.RamUso)
        ));
    }

    private static string Csv(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.0", CultureInfo.InvariantCulture)
            : "";
    }
}