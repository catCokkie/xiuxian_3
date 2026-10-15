using Godot;
using System;
using System.Linq;
using System.Text.Json;
using Xiuxian.Scripts.UI;

namespace Xiuxian.Scripts.Tests;

/// <summary>Checks real Godot imports and renders a contact sheet; never loads a player save.</summary>
public partial class ArtSmokeTest : Node
{
    private int _checks;
    private Control _canvas = null!;

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--smoke-test")) { GetTree().Quit(2); return; }
        CallDeferred(nameof(Run));
    }

    private void Check(bool valid, string description)
    {
        if (!valid) throw new InvalidOperationException(description);
        _checks++;
    }

    private Label Text(string value, Vector2 position, Vector2 size, int fontSize = 14)
    {
        var label = new Label { Text = value, Position = position, Size = size, ClipText = true };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color("4A3728"));
        _canvas.AddChild(label);
        return label;
    }

    private async void Run()
    {
        try
        {
            GetWindow().Size = new Vector2I(1440, 1280);
            _canvas = new Control { Size = new Vector2(1440, 1280) }; AddChild(_canvas);
            _canvas.AddChild(new ColorRect { Color = new Color("F3E5CA"), Size = _canvas.Size });
            Text("修仙 · 素材图鉴", new Vector2(28, 16), new Vector2(900, 42), 28);
            Text("暖棕轮廓 / 米白 / 玉绿 / 铜金 · 主角与怪物 80px，背包图标 28px", new Vector2(28, 60), new Vector2(1200, 28));
            using var file = FileAccess.Open("res://assets/asset_manifest.json", FileAccess.ModeFlags.Read);
            using var json = JsonDocument.Parse(file!.GetAsText());
            var entries = json.RootElement.GetProperty("entries").EnumerateArray().ToArray();
            Check(entries.Select(x => x.GetProperty("key").GetString()).Distinct().Count() == entries.Length, "manifest keys unique");
            float top = 105;
            foreach (string category in new[] { "characters", "item", "equipment", "ui" })
            {
                var group = entries.Where(e => category == "characters" ? new[] { "player", "monster" }.Contains(e.GetProperty("category").GetString()) : e.GetProperty("category").GetString() == category).ToArray();
                string title = category switch { "characters" => "角色与怪物", "item" => "材料、丹药、灵膳与阵法", "equipment" => "装备模板", _ => "界面与活动" };
                Text(title + $" · {group.Length}", new Vector2(28, top), new Vector2(1300, 26), 18); top += 36;
                int columns = category == "characters" ? 12 : 20;
                float width = category == "characters" ? 115 : 69;
                float height = category == "characters" ? 155 : 100;
                int index = 0;
                foreach (var entry in group)
                {
                    string key = entry.GetProperty("key").GetString()!;
                    var texture = GameArtCatalog.Get(key);
                    Check(texture != null, key + ": imported texture loads");
                    using var image = texture!.GetImage();
                    Check(!image.IsInvisible(), key + ": visible image content");
                    if (category == "characters")
                    {
                        Check(image.GetPixel(0, 0).A == 0 && image.GetPixel(image.GetWidth()-1, image.GetHeight()-1).A == 0, key + ": transparent padding");
                        Check(image.DetectAlpha() != Image.AlphaMode.None, key + ": alpha preserved");
                    }
                    Vector2 pos = new(28 + index % columns * width, top + index / columns * height);
                    var rect = new TextureRect { Texture = texture, Position = pos, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
                    _canvas.AddChild(rect);
                    rect.Size = category == "characters" ? new Vector2(80, 80) : new Vector2(width - 10, 48);
                    if (category == "characters") rect.Position += new Vector2(12, 12);
                    if (category == "characters")
                    {
                        var small = new TextureRect { Texture = texture, Position = pos + new Vector2(40, 112), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
                        _canvas.AddChild(small); small.Size = new Vector2(24, 24);
                    }
                    string id = key[(key.IndexOf(':')+1)..];
                    string label = category == "item" ? UiText.BackpackItemName(id)
                        : entry.TryGetProperty("display_name", out var display) ? display.GetString()!
                        : id.Replace("monster_", "").Replace("eq_", "").Replace("_", " ");
                    if (category == "ui" && label.Length > 10) label = label[..10];
                    var name = Text(label, pos + new Vector2(0, category == "characters" ? 136 : 54), new Vector2(width - 8, 24), 11);
                    name.TooltipText = key;
                    index++;
                }
                top += MathF.Ceiling(group.Length / (float)columns) * height + 10;
            }
            foreach (var entry in entries.Where(e => e.GetProperty("category").GetString() == "sfx"))
                Check(GD.Load<AudioStream>(entry.GetProperty("path").GetString()!) != null, "quiet cue imports");
            var fontEntry = entries.Single(e => e.GetProperty("category").GetString() == "font");
            var font = GD.Load<Font>(fontEntry.GetProperty("path").GetString()!);
            Check(font != null && "修仙气血灵草阴潮幽骨".All(c => font.HasChar(c)), "bundled font contains representative Chinese glyphs");
            Check(font is FontVariation regular && regular.VariationOpentype[2003265652].AsDouble() == 400, "UI selects regular weight instead of font's thin default");
            var coordinates = TextServerManager.GetPrimaryInterface().FontGetVariationCoordinates(font!.GetRids()[0]);
            Check(coordinates[2003265652].AsDouble() == 400, "rendered font RID applies regular weight");
            Check(GameArtCatalog.Item("future_unknown_material")?.ResourcePath.EndsWith("item_unknown_v1.svg") == true, "new item falls back to generic icon");
            Check(GameArtCatalog.Equipment("legacy_without_template", Xiuxian.Scripts.Services.EquipmentSlotType.Armor)?.ResourcePath.EndsWith("armor_v1.svg") == true, "legacy equipment falls back by slot");
            for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (OS.GetCmdlineUserArgs().Contains("--render-smoke"))
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var capture = GetViewport().GetTexture().GetImage();
                Check(capture.SavePng("res://docs/art/asset_contact_sheet.png") == Error.Ok, "contact sheet saved");
            }
            GD.Print($"ART COMPLETE: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"ART FAILURE: {error}"); GetTree().Quit(1); }
    }
}
