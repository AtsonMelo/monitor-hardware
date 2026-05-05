using System.IO;
using System.Text.Json;

public class ConfigService
{
    private readonly string configPath;

    public ConfigService(string configPath = "config.json")
    {
        this.configPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(configPath))
        {
            AppConfig defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(configPath);

        AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json);

        return config ?? new AppConfig();
    }

    private void Save(AppConfig config)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(config, options);

        File.WriteAllText(configPath, json);
    }
}
