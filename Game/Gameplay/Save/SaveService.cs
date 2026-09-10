using System;
using System.Text.Json;
using Godot;

namespace GodotGameTemplate.Gameplay.Save;

public interface ISaveService
{
    bool ExistsAtDefaultPath();

    SaveData Load();

    void Save(SaveData data);
}

/// <summary>
/// 基于 Godot FileAccess + user:// 的 JSON 存档服务。
/// </summary>
public sealed class SaveService : ISaveService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public const string DefaultSavePath = "user://savegame.json";

    public bool ExistsAtDefaultPath()
    {
        return FileAccess.FileExists(DefaultSavePath);
    }

    public SaveData Load()
    {
        if (!ExistsAtDefaultPath())
        {
            return new SaveData();
        }

        using var file = FileAccess.Open(DefaultSavePath, FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SaveData();
        }

        return JsonSerializer.Deserialize<SaveData>(json, JsonOptions) ?? new SaveData();
    }

    public void Save(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var file = FileAccess.Open(DefaultSavePath, FileAccess.ModeFlags.Write);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        file.StoreString(json);
    }
}
