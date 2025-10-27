using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace ichortower.FontSmasher;

internal class Events
{
    public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(GlyphData.BoldFontAsset)) {
            e.LoadFrom(() => {
                return new BoldFontPatchData();
            }, AssetLoadPriority.Exclusive);
        }
        else if (SpriteFonts.GameFonts.Any(a => e.Name.IsEquivalentTo(a.DataAssetName))) {
            e.LoadFrom(() => {
                return new SpriteFontPatchData();
            }, AssetLoadPriority.Exclusive);
        }
    }

    public static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        foreach (FontRef fr in SpriteFonts.GameFonts) {
            if (!e.Name.IsEquivalentTo(fr.DataAssetName)) {
                continue;
            }
            if (!SpriteFonts.PatchFont(fr, out string err)) {
                Log.Warn($"Failed to patch font '{fr.DataFieldName}' " +
                        $"using provided glyph data: {err}");
            }
            break;
        }
    }

    public static void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.Names) {
            if (name.IsEquivalentTo(GlyphData.BoldFontAsset)) {
                GlyphData.BoldFont = null;
            }
            else if (name.IsEquivalentTo(GlyphData.SpriteFont1Asset)) {
                GlyphData.SpriteFont1 = null;
            }
            else if (name.IsEquivalentTo(GlyphData.SmallFontAsset)) {
                GlyphData.SmallFont = null;
            }
            else if (name.IsEquivalentTo(GlyphData.TinyFontAsset)) {
                GlyphData.TinyFont = null;
            }
        }
    }

    public static void OnLocaleChanged(object sender, LocaleChangedEventArgs e)
    {
        string[] checks = new string[] {
            GlyphData.BoldFontAsset,
            GlyphData.SpriteFont1Asset,
            GlyphData.SmallFontAsset,
            GlyphData.TinyFontAsset,
        };
        Main.instance.Helper.GameContent.InvalidateCache((asset) => {
            return checks.Any(c => asset.Name.IsEquivalentTo(c));
        });
    }

    public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
    }
}

