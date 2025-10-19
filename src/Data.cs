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
    public BoldGlyph? Bold = null;
    //public GridGlyph? Dialogue = null;
    //public GridGlyph? DialogueColored = null;
    //public GridGlyph? Junimo = null;
    //public int? BoldSidesPadding = null;
    public AtlasGlyph? SmallFont = null;
    public AtlasGlyph? SpriteFont1 = null;
    public AtlasGlyph? TinyFont = null;
    public AtlasGlyph? OneX = null;
}

internal sealed class BoldGlyph
{
    public GridGlyph? Dialogue = null;
    public GridGlyph? Colored = null;
    public GridGlyph? Junimo = null;
    public int? LeftRightPadding = null;
}

internal sealed class GridGlyph
{
    public string? Texture = null;
    public int? SpriteIndex = null;
    public int? Baseline = null;
}

internal sealed class AtlasGlyph
{
    public string? Texture = null;
    public MetricsSource? CopyMetrics = null;
    public Rectangle? SourceRect = null;
    public GlyphPadding? Padding = null;
    public int? AboveBaseline = null;
    public int? BelowBaseline = null;
    public float? LeftSideBearing = null;
    public float? RightSideBearing = null;
}

internal sealed class GlyphPadding
{
    public int? Left = null;
    public int? Right = null;
    public int? Top = null;
    public int? Bottom = null;

    public GlyphPadding Scale(int percent) {
        return new GlyphPadding() {
            Left = this.Left * percent / 100,
            Right = this.Right * percent / 100,
            Top = this.Top * percent / 100,
            Bottom = this.Bottom * percent / 100,
        };
    }
}

internal sealed class MetricsSource
{
    public string Source = "OneX";
    public int Scale = 100;
}

#nullable disable
