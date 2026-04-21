using System.IO;
using System.Text.Json;
using OverlayChat.Client.Models;

namespace OverlayChat.Client.Services;

public static class SettingsLoader
{
    public static ClientSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ClientSettings();
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<ClientSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        return settings ?? new ClientSettings();
    }

    public static void Save(string path, ClientSettings settings)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(path, json);
    }
}
