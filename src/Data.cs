using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;

namespace ichortower.FontSmasher;

internal sealed class Glyphs
{
    internal static string DataAsset = $"{Main.ModId}/Glyphs";

    private static Dictionary<char, GlyphEntry> _Data = null;
    public static Dictionary<char, GlyphEntry> Data {
        get {
            if (_Data is null) {
                _Data = Game1.content.Load<Dictionary<char, GlyphEntry>>(DataAsset);
            }
            return _Data;
        }
        set {
            _Data = value;
        }
    }
}

#nullable enable

internal sealed class GlyphEntry
{
    public GridGlyph? Dialogue = null;
    public GridGlyph? DialogueColored = null;
    public GridGlyph? Junimo = null;
    public int BothSidesWidthOffset = 0;
    public AtlasGlyph? SmallFont = null;
    public AtlasGlyph? SpriteFont1 = null;
    public AtlasGlyph? TinyFont = null;
}

internal sealed class GridGlyph
{
    public string? Texture = null;
    public int SpriteIndex = -1;
    public int BaselineOffset = -1;
}

internal sealed class AtlasGlyph
{
    public string? Texture = null;
    public MetricsSource? CopyMetrics = null;
    public Rectangle? SourceRect = null;
    public GlyphMargins? Margins = null;
    public float? LeftSideBearing = null;
    public float? RightSideBearing = null;
}

internal sealed class GlyphMargins
{
    public int? Left = 0;
    public int? Right = 0;
    public int? Top = 0;
    public int? Bottom = 0;

    public GlyphMargins Scale(int percent) {
        return new GlyphMargins() {
            Left = this.Left * percent / 100,
            Right = this.Right * percent / 100,
            Top = this.Top * percent / 100,
            Bottom = this.Bottom * percent / 100,
        };
    }
}

internal sealed class MetricsSource
{
    public string Source = "SmallFont";
    public int Scale = 100;
}

#nullable disable
