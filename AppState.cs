using System.IO;
using System.Text.Json;

namespace DrugieOkno;

/// <summary>
/// Przechowuje stan zadań i notatek zapisywany w lokalnym pliku JSON.
/// </summary>
public class AppState
{
    public List<TodoItem> Tasks { get; set; } = new();
    public string Notes { get; set; } = string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string GetStoragePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "DrugieOkno", "state.json");
    }

    public static AppState Load()
    {
        try
        {
            var path = GetStoragePath();
            if (!File.Exists(path))
            {
                return new AppState();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save()
    {
        var path = GetStoragePath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }
}
