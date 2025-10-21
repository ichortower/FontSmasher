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
    internal static FontRef[] GameFonts = new FontRef[] {
        new("Fonts/SpriteFont1", GlyphData.SpriteFont1Asset, nameof(Game1.dialogueFont), 36),
        new("Fonts/SmallFont", GlyphData.SmallFontAsset, nameof(Game1.smallFont), 24),
        new("Fonts/TinyFont", GlyphData.TinyFontAsset, nameof(Game1.tinyFont), 21),
    };


    internal static bool PatchFont(FontRef fr, out string err)
    {
        // load from helper content manager since Game1.content's copy had its line spacing
        // altered after loading and the change persists in cache
        SpriteFont target = Main.instance.Helper.GameContent.Load<SpriteFont>(fr.GameAssetName);
        // the procedure here is "unpack the data, edit it, and reconstruct the SpriteFont",
        // since SpriteFont is hostile to editing
        List<PackItem> BoxesToPack = new();
        Dictionary<char, SpriteFont.Glyph> fontGlyphs = target.GetGlyphs();
        Dictionary<string, SpriteEntry> dataGlyphs = (Dictionary<string, SpriteEntry>)
                fr.GlyphDataProperty.GetValue(null);
        foreach (var kvp in dataGlyphs) {
            SpriteEntry which = kvp.Value;
            if (which is null) {
                continue;
            }
            char chKey = GlyphData.GetReverseKey(kvp.Key);
            if (!fontGlyphs.TryGetValue(chKey, out SpriteFont.Glyph glyph)) {
                if (which.SourceRect is null) {
                    Log.Warn($"For glyph '{kvp.Key}' ({fr.DataFieldName}): this glyph " +
                            "was not already present in the font, but the required field " +
                            "'SourceRect' was not specified. Skipping this glyph.");
                    continue;
                }
                glyph = new SpriteFont.Glyph() {
                    Character = chKey,
                };
            }

            // scalemetrics goes here
            if (which.ScaleMetrics is not null) {
                int scale = which?.ScaleMetrics ?? 100;
                if (which.LeftSideBearing is not null) {
                    which.LeftSideBearing = which.LeftSideBearing * scale / 100;
                }
                if (which.RightSideBearing is not null) {
                    which.RightSideBearing = which.RightSideBearing * scale / 100;
                }
                if (which.AboveBaseline is not null) {
                    which.AboveBaseline = which.AboveBaseline * scale / 100;
                }
                if (which.BelowBaseline is not null) {
                    which.BelowBaseline = which.BelowBaseline * scale / 100;
                }
                which.SourceRect = which.SourceRect?.Scale(scale);
                which.Padding = which.Padding?.Scale(scale);
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
            SpritePadding padding = which.Padding ?? new();
            // honor padding.top if given, but otherwise default to on-baseline
            if (which.Padding?.Top is null) {
                int dist = (which.AboveBaseline ?? (-1 * (which.BelowBaseline ?? 0)));
                padding.Top = fr.Baseline - dist - glyph.BoundsInTexture.Height;
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
                    Character = chKey,
                    Texture = which.Texture,
                    Bounds = glyph.BoundsInTexture
                });
            }

            fontGlyphs[chKey] = glyph;
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
                Log.Info(temp.ToString());
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
        int copiedSpacing = ((SpriteFont)fr.Game1Field.GetValue(Game1.game1)).LineSpacing;
        SpriteFont recons = new(sourceTex,
                boundsList,
                containerList,
                charList,
                copiedSpacing,
                target.Spacing,
                bearingList,
                target.DefaultCharacter);
        fr.Game1Field.SetValue(Game1.game1, recons);

        err = null;
        return true;
    }


    /*
     * This tries to pull up GMCM's assembly via the API, find the active menu,
     * and swap out all of the cached references to Game1.dialogueFont and
     * Game1.smallFont that SpaceShared (inexplicably) keeps on two of its
     * widget types, preventing reloading from working without closing and
     * reopening the menu.
     *
     * fieldName should be one of the DataFieldNames from the FontRef array.
     */
    internal static void TryGmcmRefUpdates(string fieldName)
    {
    }
}

internal sealed class FontRef
{
    public string GameAssetName;
    public string DataAssetName;
    public string DataFieldName;
    public int Baseline;
    public FieldInfo Game1Field;
    public PropertyInfo GlyphDataProperty;

    public FontRef(string gameAsset, string dataAsset, string fontFieldName, int baseline) {
        GameAssetName = gameAsset;
        DataAssetName = dataAsset;
        Baseline = baseline;
        DataFieldName = Path.GetFileName(dataAsset);
        Game1Field = typeof(Game1).GetField(fontFieldName,
                BindingFlags.Public | BindingFlags.Static);
        GlyphDataProperty = typeof(GlyphData).GetProperty(DataFieldName,
                BindingFlags.Public | BindingFlags.Static);
    }
}
