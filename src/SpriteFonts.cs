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
        new("Fonts/tinyFont", GlyphData.TinyFontAsset, nameof(Game1.tinyFont), 21),
    };


    /*
     * the procedure here is "unpack the data, edit it, and reconstruct the SpriteFont",
     * since SpriteFont is hostile to editing.
     * After reconstruction, an extension method actually replaces the data in the source
     * font instead of replacing the reference, in order to preserve actual and potential
     * cached references in other mods (e.g. GMCM)
     */
    internal static bool PatchFont(FontRef fr, out string err)
    {
        // load from helper content manager since Game1.content's copy had its line spacing
        // altered after loading and the change persists in cache
        SpriteFont target = Main.instance.Helper.GameContent.Load<SpriteFont>(fr.GameAssetName);
        SpriteFontPatchData modData = (SpriteFontPatchData)fr.GlyphDataProperty.GetValue(null);
        Dictionary<char, SpriteFont.Glyph> fontGlyphs = target.GetGlyphs();
        int lineSpacing = modData.Metrics?.LineSpacing ?? target.LineSpacing;
        List<PackItem> BoxesToPack = new();

        int useBaseline = modData.Metrics?.Baseline ?? -1;
        if (useBaseline == -1) {
            // get the baseline from 'A', which should be sitting on it
            if (fontGlyphs.TryGetValue('A', out SpriteFont.Glyph gA)) {
                useBaseline = gA.BoundsInTexture.Height + gA.Cropping.Y;
            }
            else {
                err = $"Can't find baseline ('A' glyph is missing!)";
                return false;
            }
        }

        foreach (var kvp in modData.Glyphs) {
            SpriteEntry which = kvp.Value;
            if (which is null) {
                continue;
            }
            char chKey = GlyphData.GetReverseKey(kvp.Key);
            bool newGlyph = false;
            if (!fontGlyphs.TryGetValue(chKey, out SpriteFont.Glyph glyph)) {
                if (which.SourceRect is null) {
                    Log.Warn($"For glyph '{kvp.Key}' ({fr.DataFieldName}): this glyph " +
                            "was not already present in the font, but the required field " +
                            "'SourceRect' was not specified. Skipping this glyph.");
                    continue;
                }
                newGlyph = true;
                glyph = new SpriteFont.Glyph() {
                    Character = chKey,
                };
            }

            // only honor global scale factor if this glyph is specifying a texture
            int thisGlyphScale = 100;
            if (which.ScaleMetrics is not null) {
                thisGlyphScale = (int)which.ScaleMetrics;
            }
            else if (which.Texture is not null && modData.Metrics.ScaleNewSources is not null) {
                thisGlyphScale = (int)modData.Metrics.ScaleNewSources;
            }

            if (newGlyph) {
                which.LeftSideBearing ??= 1f;
                which.RightSideBearing ??= 1f;
            }

            if (thisGlyphScale != 100) {
                if (which.LeftSideBearing is not null) {
                    which.LeftSideBearing = which.LeftSideBearing * thisGlyphScale / 100;
                }
                if (which.RightSideBearing is not null) {
                    which.RightSideBearing = which.RightSideBearing * thisGlyphScale / 100;
                }
                if (which.AboveBaseline is not null) {
                    which.AboveBaseline = which.AboveBaseline * thisGlyphScale / 100;
                }
                if (which.BelowBaseline is not null) {
                    which.BelowBaseline = which.BelowBaseline * thisGlyphScale / 100;
                }
                which.Padding = which.Padding?.Scale(thisGlyphScale);
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
                // bounds not scaled here because the scale is handled by box packer & renderer
                glyph.BoundsInTexture = (Rectangle)which.SourceRect;
                glyph.Width = glyph.BoundsInTexture.Width * thisGlyphScale / 100;
            }
            int thisGlyphHeight = glyph.BoundsInTexture.Height * thisGlyphScale / 100;

            SpritePadding padding = which.Padding ?? new();
            // honor padding.top if given, but otherwise default to on-baseline
            if (which.Padding?.Top is null) {
                int dist = (which.AboveBaseline ?? (-1 * (which.BelowBaseline ?? 0)));
                padding.Top = useBaseline - dist - thisGlyphHeight;
            }
            int fullHeight = (padding.Top ?? 0) + (padding.Bottom ?? 0) + thisGlyphHeight;
            // linespacing+1 is a dirty hack but so is everything else, really,
            // so what's one more crime?
            glyph.Cropping = new Rectangle(padding.Left ?? 0, padding.Top ?? 0,
                    (padding.Left ?? 0) + (padding.Right ?? 0) + (int)glyph.Width,
                    Math.Max(fullHeight, lineSpacing+1));
            glyph.Width = glyph.Cropping.Width;
            glyph.WidthIncludingBearings = glyph.LeftSideBearing + glyph.Width +
                    glyph.RightSideBearing;

            // only need to pack items that are coming from a different texture
            // this has to come after SourceRect and scaling and so on
            if (which.Texture is not null) {
                BoxesToPack.Add(new PackItem() {
                    Character = chKey,
                    Texture = which.Texture,
                    Bounds = glyph.BoundsInTexture,
                    OutputScale = thisGlyphScale,
                });
            }

            fontGlyphs[chKey] = glyph;
        }

        Texture2D sourceTex = target.Texture;
        if (BoxesToPack.Count > 0) {
            List<PackItem> packed = BoxPacker.Pack(BoxesToPack,
                    sourceTex.Width, out Rectangle size);
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
            sb.Begin(samplerState: SamplerState.PointClamp);
            sb.Draw(sourceTex,
                    position: new Vector2(0f, 0f),
                    color: Color.White);

            foreach (PackItem item in packed) {
                item.Bounds.Y += sourceTex.Height;
                //float scale = (float)item.OutputScale / 100f;
                sb.Draw(Game1.content.Load<Texture2D>(item.Texture),
                        destinationRectangle: item.Bounds,
                        //position: new Vector2((float)item.Bounds.X, (float)item.Bounds.Y),
                        sourceRectangle: item.OriginalRect,
                        color: Color.White,
                        rotation: 0f,
                        origin: new Vector2(0f, 0f),
                        //scale: new Vector2(scale, scale),
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

            // FIXME remove this before release!
            using FileStream stream = File.OpenWrite(
                    $"/home/ichortower/{fr.DataFieldName}.png");
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

        SpriteFont gameFont = (SpriteFont)fr.Game1Field.GetValue(Game1.game1);
        SpriteFont recons = new(sourceTex,
                boundsList,
                containerList,
                charList,
                gameFont.LineSpacing,
                target.Spacing,
                bearingList,
                target.DefaultCharacter);
        // see Extensions.cs
        gameFont.Snarf(recons);

        err = null;
        return true;
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
