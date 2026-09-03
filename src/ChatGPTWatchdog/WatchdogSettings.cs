using System.Text.Json;

namespace ChatGPTWatchdog;

internal sealed class WatchdogSettings
{
    public bool MonitorEnabled { get; set; } = true;
    public int CheckIntervalSeconds { get; set; } = 15;
}

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTWatchdog");

    private string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    public WatchdogSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var defaultSettings = new WatchdogSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            var settings = JsonSerializer.Deserialize<WatchdogSettings>(
                File.ReadAllText(SettingsPath),
                JsonOptions) ?? new WatchdogSettings();

            settings.CheckIntervalSeconds = Math.Clamp(settings.CheckIntervalSeconds, 5, 300);
            return settings;
        }
        catch (Exception exception)
        {
            Log.Write(DataDirectory, $"Could not load settings: {exception.Message}");
            return new WatchdogSettings();
        }
    }

    public void Save(WatchdogSettings settings)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        catch (Exception exception)
        {
            Log.Write(DataDirectory, $"Could not save settings: {exception.Message}");
        }
    }
}
