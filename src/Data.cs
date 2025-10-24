using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;

namespace ichortower.FontSmasher;

internal sealed class GlyphData
{
    internal static string BoldFontAsset = $"{Main.ModId}/BoldFont";
    internal static string SpriteFont1Asset = $"{Main.ModId}/SpriteFont1";
    internal static string SmallFontAsset = $"{Main.ModId}/SmallFont";
    internal static string TinyFontAsset = $"{Main.ModId}/TinyFont";

    private static Dictionary<string, BoldEntry> _BoldFontData = null;
    private static SpriteFontPatchData _SpriteFont1Data = null;
    private static SpriteFontPatchData _SmallFontData = null;
    private static SpriteFontPatchData _TinyFontData = null;

    public static Dictionary<string, BoldEntry> BoldFont {
        get {
            if (_BoldFontData is null) {
                _BoldFontData = Game1.content.Load
                        <Dictionary<string, BoldEntry>>(BoldFontAsset);
            }
            return _BoldFontData;
        }
        set {
            _BoldFontData = value;
        }
    }

    public static SpriteFontPatchData SpriteFont1 {
        get {
            if (_SpriteFont1Data is null) {
                _SpriteFont1Data = Game1.content.Load<SpriteFontPatchData>(SpriteFont1Asset);
            }
            return _SpriteFont1Data;
        }
        set {
            _SpriteFont1Data = value;
        }
    }

    public static SpriteFontPatchData SmallFont {
        get {
            if (_SmallFontData is null) {
                _SmallFontData = Game1.content.Load<SpriteFontPatchData>(SmallFontAsset);
            }
            return _SmallFontData;
        }
        set {
            _SmallFontData = value;
        }
    }

    public static SpriteFontPatchData TinyFont {
        get {
            if (_TinyFontData is null) {
                _TinyFontData = Game1.content.Load<SpriteFontPatchData>(TinyFontAsset);
            }
            return _TinyFontData;
        }
        set {
            _TinyFontData = value;
        }
    }

    public static string GetKey(char c) {
        if (c == ' ') {
            return "<Space>";
        }
        else if (c == ' ') {
            return "<Nbsp>";
        }
        return c.ToString();
    }

    public static char GetReverseKey(string s) {
        if (s == "<Space>") {
            return ' ';
        }
        else if (s == "<Nbsp>") {
            return ' ';
        }
        return s[0];
    }
}


#nullable enable

internal sealed class BoldEntry
{
    public BoldGlyph? Dialogue = null;
    public BoldGlyph? Colored = null;
    public BoldGlyph? Junimo = null;
    public int? LeftRightPadding = null;
}

internal sealed class BoldGlyph
{
    public string? Texture = null;
    public int? SpriteIndex = null;
    public int? Baseline = null;
}

internal sealed class SpriteFontPatchData
{
    public SpriteFontMetrics Metrics = new();
    public Dictionary<string, SpriteEntry> Glyphs = new();
}

internal sealed class SpriteFontMetrics
{
    public int? Baseline = null;
    public int? LineSpacing = null;
    public int? ScaleNewSources = null;
}

internal sealed class SpriteEntry
{
    public string? Texture = null;
    public int? ScaleMetrics = null;
    public Rectangle? SourceRect = null;
    public SpritePadding? Padding = null;
    public int? AboveBaseline = null;
    public int? BelowBaseline = null;
    public float? LeftSideBearing = null;
    public float? RightSideBearing = null;
}

internal sealed class SpritePadding
{
    public int? Left = null;
    public int? Right = null;
    public int? Top = null;
    public int? Bottom = null;

    public SpritePadding Scale(int percent) {
        return new SpritePadding() {
            Left = this.Left * percent / 100,
            Right = this.Right * percent / 100,
            Top = this.Top * percent / 100,
            Bottom = this.Bottom * percent / 100,
        };
    }
}

#nullable disable
