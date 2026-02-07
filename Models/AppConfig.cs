using System.IO;
using System.Text.Json;

namespace ChordBox.Models;

public class AppConfig
{
    public List<StrumPatternData> CustomStrumPatterns { get; set; } = new();
    public List<string> RecentSoundFonts { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string ConfigPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChordBox", "config.json");

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static AppConfig Load()
    {
        try
        {
            var path = ConfigPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var path = ConfigPath;
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, ToJson());
        }
        catch { }
    }
}
