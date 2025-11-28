using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using System.Collections.Generic;
using System.Globalization;

namespace ichortower.FontSmasher;

internal sealed class TextColors
{
    internal static string ColorDataAsset = $"{Main.ModId}/SpriteFontColors";

    public static ColorSet Vanilla = new ColorSet {
        Text = Game1.textColor,
        Shadow = Game1.textShadowColor,
        DarkShadow = Game1.textShadowDarkerColor,
        Unselected = Game1.unselectedOptionColor,
    };

    private static SpriteFontColorValues _Data = null;
    public static SpriteFontColorValues Data {
        get {
            _Data ??= Game1.content.Load<SpriteFontColorValues>(ColorDataAsset);
            return _Data;
        }
        set {
            _Data = value;
        }
    }

    internal static void SaveColors()
    {
        Game1.textColor = (Color)(ColorFromString(Data.Text) ?? Vanilla.Text);
        Game1.textShadowColor = (Color)(ColorFromString(Data.Shadow) ?? Vanilla.Shadow);
        Game1.textShadowDarkerColor = (Color)(ColorFromString(Data.DarkShadow) ?? Vanilla.DarkShadow);
        Game1.unselectedOptionColor = (Color)(ColorFromString(Data.Unselected) ?? Vanilla.Unselected);
    }

    internal static void ResetColors()
    {
        Game1.textColor = (Color)Vanilla.Text;
        Game1.textShadowColor = (Color)Vanilla.Shadow;
        Game1.textShadowDarkerColor = (Color)Vanilla.DarkShadow;
        Game1.unselectedOptionColor = (Color)Vanilla.Unselected;
    }

#nullable enable

    /*
     * accepts:
     *  #rrggbb
     *  #rrggbbaa
     *  rgb(r, g, b) (ints 0-255)
     *  rgba(r, g, b, a) (ints 0-255)
     *  plain color name (tries to find property on Color class)
     */
    internal static Color? ColorFromString(string s)
    {
        if (s is null) {
            return null;
        }
        // r, g, b, a, in order
        int[] values = { 0, 0, 0, -1 };
        if (s.StartsWith("#")) {
            if (s.Length < 7) {
                Log.Warn($"Color value too short: '{s}'");
                return null;
            }
            for (int i = 0; i < 3; ++i) {
                if (!int.TryParse(s.Substring(1+2*i, 2), NumberStyles.HexNumber, null, out values[i])) {
                    Log.Warn($"Couldn't parse color value '{s}'");
                    return null;
                }
            }
            if (s.Length >= 9) {
                if (!int.TryParse(s.Substring(7, 2), NumberStyles.HexNumber, null, out values[3])) {
                    Log.Warn($"Couldn't parse color value '{s}'");
                    return null;
                }
            }
            Color c = new(values[0], values[1], values[2]);
            if (values[3] != -1) {
                c.A = (byte)values[3];
            }
            Log.Warn(c.ToString());
            return c;
        }
        if (s.Substring(0, 3).EqualsIgnoreCase("rgb")) {
        }
        return null;
    }
}

internal sealed class ColorSet
{
    public Color? Text = null;
    public Color? Shadow = null;
    public Color? DarkShadow = null;
    public Color? Unselected = null;
}

internal sealed class SpriteFontColorValues
{
    public string? Text = null;
    public string? Shadow = null;
    public string? DarkShadow = null;
    public string? Unselected = null;
    public Dictionary<string, string> Categories = new();
}

#nullable disable
