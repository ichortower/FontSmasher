using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ichortower.FontSmasher;

internal sealed class SpriteFonts
{
    /*
     * Keep these arrays in sync, except note that DataFieldNames has the special extra value
     * 'OneX', which is not parsed on its own but serves only as a 1x source for CopyMetrics
     */
    internal static FieldInfo[] FontFields = new [] {
        typeof(Game1).GetField(nameof(Game1.dialogueFont),
                BindingFlags.Public | BindingFlags.Static),
        typeof(Game1).GetField(nameof(Game1.smallFont),
                BindingFlags.Public | BindingFlags.Static),
        typeof(Game1).GetField(nameof(Game1.tinyFont),
                BindingFlags.Public | BindingFlags.Static),
    };
    internal static string[] AssetNames = new [] {
        "Fonts/SpriteFont1",
        "Fonts/SmallFont",
        "Fonts/tinyFont",
    };
    internal static int[] Baselines = new [] {
        36,
        24,
        21,
    };
    internal static string[] DataFieldNames = new [] {
        nameof(GlyphEntry.SpriteFont1),
        nameof(GlyphEntry.SmallFont),
        nameof(GlyphEntry.TinyFont),
        nameof(GlyphEntry.OneX),
    };
    // this array is automatic tho
    internal static FieldInfo[] DataFields = DataFieldNames.Select((name) => {
        return typeof(GlyphEntry).GetField(name,
                BindingFlags.Public | BindingFlags.Instance);
    }).ToArray();

    public static void PatchIn()
    {
        for (int i = 0; i < FontFields.Length; ++i) {
            if (!PatchFont(i, out string err)) {
                Log.Warn($"Failed to patch font '{DataFields[i].Name}' " +
                        $"using provided glyph data: {err}");
            }
        }
    }

    internal static bool PatchFont(int index, out string err)
    {
        // load from helper content manager since Game1.content's copy had its line spacing
        // altered after loading and the change persists in cache
        SpriteFont target = Main.instance.Helper.GameContent.Load<SpriteFont>(AssetNames[index]);
        string fontName = DataFields[index].Name;
        // the procedure here is "unpack the data, edit it, and reconstruct the SpriteFont",
        // since SpriteFont is hostile to editing
        List<PackItem> BoxesToPack = new();
        Dictionary<char, SpriteFont.Glyph> fontGlyphs = target.GetGlyphs();
        foreach (var kvp in Glyphs.Data) {
            GlyphEntry entry = kvp.Value;
            AtlasGlyph which = (AtlasGlyph)DataFields[index].GetValue(entry);
            if (which is null) {
                continue;
            }
            if (!fontGlyphs.TryGetValue(kvp.Key, out SpriteFont.Glyph glyph)) {
                if (which.SourceRect is null) {
                    Log.Warn($"For glyph '{kvp.Key}' ({fontName}): this glyph was not already " +
                            "present in the font, but the required field 'SourceRect' " +
                            "was not specified. Skipping this glyph.");
                    continue;
                }
                glyph = new SpriteFont.Glyph() {
                    Character = kvp.Key,
                };
            }

            // sub out our source for numerical stuff if needed, but leave texture alone
            if (which.CopyMetrics is not null) {
                try {
                    int i = Array.IndexOf(DataFieldNames, which.CopyMetrics.Source);
                    AtlasGlyph fromObj = (AtlasGlyph)DataFields[i].GetValue(entry);
                    if (fromObj.LeftSideBearing is not null) {
                        which.LeftSideBearing = fromObj.LeftSideBearing *
                                which.CopyMetrics.Scale / 100;
                    }
                    if (fromObj.RightSideBearing is not null) {
                        which.RightSideBearing = fromObj.RightSideBearing *
                                which.CopyMetrics.Scale / 100;
                    }
                    if (fromObj.AboveBaseline is not null) {
                        which.AboveBaseline = fromObj.AboveBaseline *
                                which.CopyMetrics.Scale / 100;
                    }
                    else if (fromObj.BelowBaseline is not null) {
                        which.BelowBaseline = fromObj.BelowBaseline *
                                which.CopyMetrics.Scale / 100;
                    }
                    which.SourceRect = fromObj.SourceRect?.Scale(which.CopyMetrics.Scale);
                    which.Padding = fromObj.Padding?.Scale(which.CopyMetrics.Scale);
                }
                catch (Exception e) {
                    Log.Warn($"For glyph '{kvp.Key}' ({fontName}): this glyph failed to " +
                            $"copy metrics from target '{which.CopyMetrics.Source}'. " +
                            $"Skipping this glyph. {e}");
                    continue;
                }
            }

            if (which.LeftSideBearing is not null) {
                glyph.LeftSideBearing = (float)which.LeftSideBearing;
            }
            if (which.RightSideBearing is not null) {
                glyph.RightSideBearing = (float)which.RightSideBearing;
            }
            // for new glyphs, this will always be defined (see above warning), but existing ones
            // might (should?) leave it alone
            if (which.SourceRect is not null) {
                glyph.BoundsInTexture = (Rectangle)which.SourceRect;
                // this 16 is bad but it shouldn't be possible to fall back to it
                glyph.Width = which.SourceRect?.Width ?? 16f;
            }
            GlyphPadding padding = which.Padding ?? new();
            // honor padding.top if given, but otherwise default to on-baseline
            if (which.Padding?.Top is null) {
                int dist = (which.AboveBaseline ?? (-1 * (which.BelowBaseline ?? 0)));
                padding.Top = Baselines[index] - dist - glyph.BoundsInTexture.Height;
            }
            int fullHeight = padding.Top ?? 0 + padding.Bottom ?? 0 + glyph.BoundsInTexture.Height;
            glyph.Cropping = new Rectangle(padding.Left ?? 0, padding.Top ?? 0,
                    (padding.Left ?? 0) + (padding.Right ?? 0) + (int)glyph.Width,
                    Math.Max(fullHeight, target.LineSpacing));
            glyph.Width = glyph.Cropping.Width;
            glyph.WidthIncludingBearings = glyph.LeftSideBearing + glyph.Width +
                    glyph.RightSideBearing;
            // texture check comes at the end since sourcerect may or may not be in our data
            if (which.Texture is not null) {
                BoxesToPack.Add(new PackItem() {
                    Character = kvp.Key,
                    Texture = which.Texture,
                    Bounds = glyph.BoundsInTexture
                });
            }

            fontGlyphs[kvp.Key] = glyph;
        }

        Texture2D sourceTex = target.Texture;
        if (BoxesToPack.Count > 0) {
            List<PackItem> packed = BoxPacker.Pack(BoxesToPack, out Rectangle size);
            RenderTarget2D render = new(Game1.graphics.GraphicsDevice,
                    Math.Max(sourceTex.Width, size.Width),
                    sourceTex.Height + size.Height);

            // save current render target for restoration later
            RenderTarget2D savedTarget = null;
            RenderTargetBinding[] rt = Game1.graphics.GraphicsDevice.GetRenderTargets();
            if (rt.Length > 0) {
                savedTarget = rt[0].RenderTarget as RenderTarget2D;
            }
            SpriteBatch sb = new(Game1.graphics.GraphicsDevice);
            Game1.graphics.GraphicsDevice.SetRenderTarget(render);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin();
            sb.Draw(sourceTex,
                    position: new Vector2(0f, 0f),
                    color: Color.White);

            foreach (PackItem item in packed) {
                item.Bounds.Y += sourceTex.Height;
                sb.Draw(Game1.content.Load<Texture2D>(item.Texture),
                        position: new Vector2((float)item.Bounds.X, (float)item.Bounds.Y),
                        sourceRectangle: item.OriginalRect,
                        color: Color.White,
                        rotation: 0f,
                        origin: new Vector2(0f, 0f),
                        scale: new Vector2(1f, 1f),
                        effects: SpriteEffects.None,
                        layerDepth: 1f);
                // boo struct
                SpriteFont.Glyph temp = fontGlyphs[item.Character];
                temp.BoundsInTexture = item.Bounds;
                fontGlyphs[item.Character] = temp;
            }
            sb.End();
            Game1.graphics.GraphicsDevice.SetRenderTarget(savedTarget);
            sourceTex = render as Texture2D;
            using FileStream stream = File.OpenWrite("/home/ichortower/SpriteFont1.png");
            sourceTex.SaveAsPng(stream, sourceTex.Width, sourceTex.Height);
        }

        List<Rectangle> boundsList = new();
        List<Rectangle> containerList = new();
        List<char> charList = new();
        List<Vector3> bearingList = new();
        List<SpriteFont.Glyph> ascend = fontGlyphs.Values.ToList();
        ascend.Sort((a, b) => a.Character - b.Character);
        foreach (SpriteFont.Glyph g in ascend) {
            boundsList.Add(g.BoundsInTexture);
            containerList.Add(g.Cropping);
            charList.Add(g.Character);
            bearingList.Add(new(g.LeftSideBearing, g.Width, g.RightSideBearing));
        }
        // preserve Game1's mutated line spacings
        int copiedSpacing = ((SpriteFont)FontFields[index].GetValue(Game1.game1)).LineSpacing;
        SpriteFont recons = new(sourceTex,
                boundsList,
                containerList,
                charList,
                copiedSpacing,
                target.Spacing,
                bearingList,
                target.DefaultCharacter);
        FontFields[index].SetValue(Game1.game1, recons);

        err = null;
        return true;
    }
}
