using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace ichortower.FontSmasher;

internal class Events
{
    public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Glyphs.DataAsset)) {
            e.LoadFrom(() => {
                return new Dictionary<char, GlyphEntry>();
            }, AssetLoadPriority.Exclusive);
        }
    }

    public static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Glyphs.DataAsset)) {
            SpriteFonts.PatchIn();
        }
    }

    public static void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.Names) {
            if (name.IsEquivalentTo(Glyphs.DataAsset)) {
                Log.Trace("Invalidating cache");
                Glyphs.Data = null;
            }
        }
    }

    public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Main.CPAPI.IsConditionsApiReady) {
            return;
        }
        _ = Glyphs.Data;
        Main.instance.Helper.Events.GameLoop.UpdateTicked -= Events.OnUpdateTicked;
    }

    public static void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        // register GMCM entries here
    }

    public static void OnDayStarted(object sender, DayStartedEventArgs e)
    {
    }

    public static void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
    }
}

