namespace monitor_hardware.Tests;

public class HtmlReportServiceTests
{
    [Fact]
    public void GenerateHistoricalReport_WhenMultipleCsvFilesExist_ConsolidatesHistory()
    {
        string tempDirectory = CreateTempDirectory();
        string logsDirectory = Path.Combine(tempDirectory, "logs");
        string reportsDirectory = Path.Combine(tempDirectory, "reports");

        try
        {
            Directory.CreateDirectory(logsDirectory);

            WriteCsv(logsDirectory, "monitor-hardware-20260504.csv", "2026-05-04 23:59:58,45.0,12.0,10.0,1500.0,55.0,0.0,30.0,40.0,54.0");
            WriteCsv(logsDirectory, "monitor-hardware-20260505.csv", "2026-05-05 00:00:02,47.0,18.0,12.0,1510.0,56.0,2.0,31.0,41.0,55.0");

            HtmlReportService htmlReportService = new HtmlReportService();

            string reportPath = htmlReportService.GenerateHistoricalReport(logsDirectory, reportsDirectory);
            string html = File.ReadAllText(reportPath);

            Assert.Equal(Path.Combine(reportsDirectory, "monitor-hardware-historico.html"), reportPath);
            Assert.Contains("2 arquivos CSV", html);
            Assert.Contains("monitor-hardware-20260504.csv", html);
            Assert.Contains("monitor-hardware-20260505.csv", html);
            Assert.Contains("Tabela com todas as 2 leituras", html);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void WriteCsv(string logsDirectory, string fileName, string entry)
    {
        string csvPath = Path.Combine(logsDirectory, fileName);

        File.WriteAllLines(csvPath, new[]
        {
            "DataHora,CPU_Temp_C,CPU_Uso_Percent,CPU_Power_W,CPU_Fan_RPM,GPU_Temp_C,GPU_Uso_Percent,GPU_Power_W,SSD_Temp_C,RAM_Uso_Percent",
            entry
        });
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "monitor-hardware-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }
}
