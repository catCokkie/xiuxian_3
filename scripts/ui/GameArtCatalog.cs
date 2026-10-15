using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.UI;

/// <summary>Stable content keys; generated equipment instance IDs never select artwork.</summary>
public static class GameArtCatalog
{
    private static readonly Dictionary<string, string> Paths = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.Ordinal);
    private static bool _loaded;

    public static Texture2D? Get(string key)
    {
        if (!_loaded)
        {
            _loaded = true;
            using var file = FileAccess.Open("res://assets/asset_manifest.json", FileAccess.ModeFlags.Read);
            if (file != null)
            {
                using var data = JsonDocument.Parse(file.GetAsText());
                foreach (var entry in data.RootElement.GetProperty("entries").EnumerateArray())
                    Paths[entry.GetProperty("key").GetString()!] = entry.GetProperty("path").GetString()!;
            }
        }
        return Paths.TryGetValue(key, out string? path) ? LoadTexture(path) : null;
    }

    public static Texture2D? LoadTexture(string path)
    {
        if (Textures.TryGetValue(path, out var texture)) return texture;
        if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) && FileAccess.FileExists(path) && !FileAccess.FileExists(path + ".import"))
        {
            using var source = new Image();
            if (source.LoadSvgFromString(FileAccess.GetFileAsString(path)) != Error.Ok) return null;
            var vectorTexture = ImageTexture.CreateFromImage(source);
            vectorTexture.TakeOverPath(path); Textures[path] = vectorTexture; return vectorTexture;
        }
        if (!ResourceLoader.Exists(path)) return null;
        var loaded = GD.Load<Texture2D>(path);
        if (loaded != null) Textures[path] = loaded;
        return loaded;
    }

    public static Texture2D? Item(string itemId) => Get("item:" + itemId) ?? Get("ui:item_unknown");

    public static Texture2D? Equipment(string templateId, EquipmentSlotType slot) =>
        Get("equipment:" + templateId) ?? Get("ui:" + slot.ToString().ToLowerInvariant()) ?? Get("ui:item_unknown");
}
