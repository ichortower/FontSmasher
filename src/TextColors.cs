using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ichortower.FontSmasher;

internal sealed class TextColors
{
    internal static string ColorDataAsset = $"{Main.ModId}/SpriteFontColors";

    internal static readonly Dictionary<string, Color> Vanilla = new() {
        {"Text", Game1.textColor},
        {"Shadow", Game1.textShadowColor},
        {"DarkShadow", Game1.textShadowDarkerColor},
        {"Unselected", Game1.unselectedOptionColor},
    };

    private static Dictionary<string, string> _Data = null;
    internal static Dictionary<string, string> Data {
        get {
            _Data ??= Game1.content.Load<Dictionary<string,string>>(ColorDataAsset);
            return _Data;
        }
        set {
            _Data = value;
        }
    }

    internal static void SaveColors()
    {
        Game1.textColor = GetDataColor("Text") ?? Vanilla["Text"];
        Game1.textShadowColor = GetDataColor("Shadow") ?? Vanilla["Shadow"];
        Game1.textShadowDarkerColor = GetDataColor("DarkShadow") ?? Vanilla["DarkShadow"];
        Game1.unselectedOptionColor = GetDataColor("Unselected") ?? Vanilla["Unselected"];
    }

    internal static void ResetColors()
    {
        Game1.textColor = Vanilla["Text"];
        Game1.textShadowColor = Vanilla["Shadow"];
        Game1.textShadowDarkerColor = Vanilla["DarkShadow"];
        Game1.unselectedOptionColor = Vanilla["Unselected"];
    }

#nullable enable

    internal static Color? GetDataColor(string key)
    {
        return Data.TryGetValue(key, out string? val) ? ColorFromString(val) : null;
    }

    /*
     * accepts:
     *  #rrggbb
     *  #rrggbbaa
     *  rgb(r, g, b) (ints 0-255)
     *  rgba(r, g, b, a) (ints 0-255)
     *  @ColorName (tries to find property on Color class)
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
            for (int i = 0; i < 4; ++i) {
                if (1+2*(i+1) > s.Length) {
                    break;
                }
                if (!int.TryParse(s.Substring(1+2*i, 2), NumberStyles.HexNumber, null, out values[i])) {
                    Log.Warn($"Couldn't parse color value '{s}'");
                    return null;
                }
            }
            Color c = new(values[0], values[1], values[2]);
            if (values[3] != -1) {
                c.A = (byte)values[3];
            }
            return c;
        }
        if (s.Substring(0, 3).EqualsIgnoreCase("rgb")) {
            char[] trimmings = {'a','A','(',')',' '};
            string[] args = s.Substring(3).Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim(trimmings)).ToArray();
            if (args.Length < 3) {
                Log.Warn($"Color value not readable as rgb(a): '{s}'");
                return null;
            }
            for (int i = 0; i < 4; ++i) {
                if (i >= args.Length) {
                    break;
                }
                if (!int.TryParse(args[i], out values[i])) {
                    Log.Warn($"Couldn't parse color value '{s}'");
                    return null;
                }
            }
            Color c = new(values[0], values[1], values[2]);
            if (values[3] != -1) {
                c.A = (byte) values[3];
            }
            return c;
        }
        if (s.StartsWith("@")) {
            PropertyInfo? item = typeof(Color).GetProperty(
                    s.Substring(1), BindingFlags.Public | BindingFlags.Static);
            if (item is not null) {
                return (Color)item.GetValue(null)!;
            }
        }
        return null;
    }
}

#nullable disable
