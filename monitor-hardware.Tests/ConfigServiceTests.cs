using System.Text.Json;

namespace monitor_hardware.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void Load_WhenConfigFileDoesNotExist_CreatesDefaultConfig()
    {
        string tempDirectory = CreateTempDirectory();
        string configPath = Path.Combine(tempDirectory, "config.json");

        try
        {
            ConfigService configService = new ConfigService(configPath);

            AppConfig config = configService.Load();

            Assert.True(File.Exists(configPath));
            Assert.Equal(80, config.CpuTempMax);
            Assert.Equal(80, config.GpuTempMax);
            Assert.Equal(60, config.SsdTempMax);
            Assert.Equal(2000, config.IntervaloMs);
            Assert.True(config.EnableCsv);
            Assert.True(config.EnableConsole);
            Assert.Equal("resumo", config.Mode);
            Assert.Equal("Fan #2", config.CpuFanSensorName);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenConfigFileExists_LoadsCustomConfig()
    {
        string tempDirectory = CreateTempDirectory();
        string configPath = Path.Combine(tempDirectory, "config.json");

        try
        {
            AppConfig customConfig = new AppConfig
            {
                CpuTempMax = 75,
                GpuTempMax = 82,
                SsdTempMax = 55,
                IntervaloMs = 1000,
                EnableCsv = false,
                EnableConsole = true,
                Mode = "resumo",
                CpuFanSensorName = "Fan #3"
            };

            string json = JsonSerializer.Serialize(customConfig);
            File.WriteAllText(configPath, json);

            ConfigService configService = new ConfigService(configPath);

            AppConfig config = configService.Load();

            Assert.Equal(75, config.CpuTempMax);
            Assert.Equal(82, config.GpuTempMax);
            Assert.Equal(55, config.SsdTempMax);
            Assert.Equal(1000, config.IntervaloMs);
            Assert.False(config.EnableCsv);
            Assert.True(config.EnableConsole);
            Assert.Equal("resumo", config.Mode);
            Assert.Equal("Fan #3", config.CpuFanSensorName);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "monitor-hardware-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }
}
