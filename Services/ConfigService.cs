using System.IO;
using System.Text.Json;

class ConfigService
{
    private const string ConfigPath = "config.json";

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            AppConfig defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(ConfigPath);

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

        File.WriteAllText(ConfigPath, json);
    }
}