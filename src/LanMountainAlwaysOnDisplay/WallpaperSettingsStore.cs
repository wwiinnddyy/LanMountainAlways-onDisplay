using System.IO;
using System.Text.Json;

namespace LanMountainAlwaysOnDisplay;

public sealed class WallpaperSettingsStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public WallpaperSettingsStore()
        : this(GetDefaultSettingsPath())
    {
    }

    public WallpaperSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public WallpaperSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return WallpaperSettings.Default;
            }

            var json = File.ReadAllText(_settingsPath);
            return (JsonSerializer.Deserialize<WallpaperSettings>(json, s_jsonOptions) ?? WallpaperSettings.Default).Normalize();
        }
        catch
        {
            return WallpaperSettings.Default;
        }
    }

    public void Save(WallpaperSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings.Normalize(), s_jsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LanMountain", "AlwaysOnDisplay", "settings.json");
    }
}
