using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ichortower.FontSmasher;

internal class Patches
{
    public static void Apply()
    {
        Harmony harmony = new(Main.ModId);
        PatchMethod(harmony, typeof(StardewValley.BellsAndWhistles.SpriteText),
                nameof(StardewValley.BellsAndWhistles.SpriteText.getWidthOffsetForChar),
                null,
                nameof(Patches.SpriteText_getWidthOffsetForChar_Postfix));
        PatchMethod(harmony, typeof(SpriteText),
                nameof(SpriteText.positionOfNextSpace),
                null,
                nameof(Patches.SpriteText_positionOfNextSpace_Transpiler));
        PatchMethod(harmony, typeof(StardewValley.BellsAndWhistles.SpriteText),
                "drawString",
                null,
                nameof(Patches.SpriteText_drawString_Transpiler));
        // have to patch four separate SpriteBatch.DrawStrings because they all have
        // their own implementations
        PatchMethod(harmony, typeof(SpriteBatch),
                nameof(SpriteBatch.DrawString),
                new[]{typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color)},
                nameof(Patches.SpriteBatch_DrawString_Prefix));
        PatchMethod(harmony, typeof(SpriteBatch),
                nameof(SpriteBatch.DrawString),
                new[]{typeof(SpriteFont), typeof(string), typeof(Vector2), typeof(Color),
                    typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects),
                    typeof(float)},
                nameof(Patches.SpriteBatch_DrawString_Prefix));
        PatchMethod(harmony, typeof(SpriteBatch),
                nameof(SpriteBatch.DrawString),
                new[]{typeof(SpriteFont), typeof(StringBuilder), typeof(Vector2), typeof(Color)},
                nameof(Patches.SpriteBatch_DrawString_Prefix));
        PatchMethod(harmony, typeof(SpriteBatch),
                nameof(SpriteBatch.DrawString),
                new[]{typeof(SpriteFont), typeof(StringBuilder), typeof(Vector2), typeof(Color),
                    typeof(float), typeof(Vector2), typeof(Vector2), typeof(SpriteEffects),
                    typeof(float)},
                nameof(Patches.SpriteBatch_DrawString_Prefix));
    }


    internal static void SpriteBatch_DrawString_Prefix(
            SpriteFont spriteFont)
    {
        // these look odd, but accessing the GlyphData objects triggers a load (synchronous),
        // and when the load completes it fires AssetReady which patches the font in-place
        if (System.Object.ReferenceEquals(spriteFont, Game1.dialogueFont)) {
            _ = GlyphData.SpriteFont1;
        }
        else if (System.Object.ReferenceEquals(spriteFont, Game1.smallFont)) {
            _ = GlyphData.SmallFont;
        }
        else if (System.Object.ReferenceEquals(spriteFont, Game1.tinyFont)) {
            _ = GlyphData.TinyFont;
        }
    }

    internal static void SpriteText_getWidthOffsetForChar_Postfix(
            char c, ref int __result)
    {
        if (GlyphData.BoldFont.Glyphs.TryGetValue(GlyphData.GetKey(c), out var val) &&
                (val?.LeftRightPadding ?? -1) >= 0) {
            __result = -1 * (int)val.LeftRightPadding;
        }
    }

    private static MethodInfo _mgsrfc = null;
    internal static MethodInfo Method_getSourceRectForChar {
        get {
            if (_mgsrfc is null) {
                _mgsrfc = typeof(SpriteText).GetMethod("getSourceRectForChar",
                        BindingFlags.NonPublic | BindingFlags.Static);
            }
            return _mgsrfc;
        }
        set {
            _mgsrfc = value;
        }
    }

    internal static IEnumerable<CodeInstruction> SpriteText_positionOfNextSpace_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        CodeMatcher cm = new(instructions);

        // instead of checking previous? character's offset, account for both sides of this one
        // TODO this can't just be a dup once we support asymmetrical bearings
        cm.MatchStartForward(
            new (OpCodes.Ldarg_0),
            new (OpCodes.Ldc_I4_0),
            new (OpCodes.Ldloc_S),
            new (OpCodes.Ldc_I4_1))
        .RemoveInstructions(8)
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Dup));
        return cm.InstructionEnumeration();
    }

    internal static IEnumerable<CodeInstruction> SpriteText_drawString_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        LocalBuilder sourceTexture = generator.DeclareLocal(typeof(Texture2D));
        MethodInfo GetSourceForChar = typeof(Patches).GetMethod(nameof(Patches.GetSourceForChar),
                BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo ShiftDrawStart = typeof(Patches).GetMethod(nameof(Patches.ShiftDrawStart),
                BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo ShiftDrawEnd = typeof(Patches).GetMethod(nameof(Patches.ShiftDrawEnd),
                BindingFlags.NonPublic | BindingFlags.Static);
        LocalBuilder yOffset = generator.DeclareLocal(typeof(float));
        FieldInfo Vector2XField = typeof(Vector2).GetField(nameof(Vector2.X),
                BindingFlags.Public | BindingFlags.Instance);
        FieldInfo Vector2YField = typeof(Vector2).GetField(nameof(Vector2.Y),
                BindingFlags.Public | BindingFlags.Instance);

        CodeMatcher cm = new(instructions);

        // remove the code that calculates the baseline offset for font_bold. our call calculates
        // it to allow for added glyphs, so it's a waste of effort to keep the vanilla calc
        cm.MatchStartForward(
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldloc_S),
            new (OpCodes.Callvirt),
            new (OpCodes.Call),
            new (OpCodes.Brtrue_S))
        // see Extensions.cs
        .RemoveUntilForward(
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldloc_S),
            new (OpCodes.Ldloc_2),
            new (OpCodes.Ldfld),
            new (OpCodes.Conv_I4),
            new (OpCodes.Ldarg_2))

        // replace the call to SpriteText.getSourceRectForChar with a call to our GetSourceForChar.
        // ours returns values via out parameters, so the original stloc is removed
        .MatchStartForward(
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldloc_S),
            new (OpCodes.Callvirt),
            new (OpCodes.Ldarg_S),
            new (OpCodes.Call),
            new (OpCodes.Stloc_S))
        .Advance(3)
        .RemoveInstructions(3)
        .InsertAndAdvance(
            new (OpCodes.Ldloc_S, 0),
            new (OpCodes.Ldarg_S, 9),
            new (OpCodes.Ldloca_S, sourceTexture),
            new (OpCodes.Ldloca_S, 16),
            new (OpCodes.Ldloca_S, yOffset),
            new (OpCodes.Call, GetSourceForChar))
        // adjust draw position for the current character (minus left-side offset)
        .InsertAndAdvance(
            new (OpCodes.Ldloca_S, 2),
            new (OpCodes.Ldflda, Vector2XField),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldloc_S, 12),
            new (OpCodes.Call, ShiftDrawStart))
        // preserve ldarg.0 for the eventual draw call
        .Advance(1)
        // remove texture selection and use our baseline offset and texture instead
        .RemoveInstructions(5)
        .InsertAndAdvance(
            new (OpCodes.Ldloca_S, 15),
            new (OpCodes.Ldloc_S, yOffset),
            new (OpCodes.Stfld, Vector2YField),
            new (OpCodes.Ldloc_S, sourceTexture))
        // replace default width-adjust calculation (weird, bad) with ours (differently bad)
        .MatchStartForward(
            new (OpCodes.Callvirt),
            new (OpCodes.Ldloc_S))
        .Advance(1)
        .RemoveUntilForward(
            new (OpCodes.Ldloc_S),
            new (OpCodes.Stsfld))
        .InsertAndAdvance(
            new (OpCodes.Ldloca_S, 2),
            new (OpCodes.Ldflda, Vector2XField),
            new (OpCodes.Ldarg_1),
            new (OpCodes.Ldloc_S, 12),
            new (OpCodes.Call, ShiftDrawEnd));

        return cm.InstructionEnumeration();
    }


    /*
     */
    internal static void GetSourceForChar(char c, bool coloredText, bool junimoText,
            out Texture2D sourceTexture, out Rectangle sourceRect, out float baselineOffset)
    {
        BoldGlyph found = null;
        if (GlyphData.BoldFont.Glyphs.TryGetValue(GlyphData.GetKey(c), out var entry)) {
            if (coloredText && entry?.Colored is not null) {
                found = entry.Colored;
            }
            else if (junimoText && entry?.Junimo is not null) {
                found = entry.Junimo;
            }
            else if (entry?.Dialogue is not null) {
                found = entry.Dialogue;
            }
        }

        if (found?.Texture is not null && (found?.SpriteIndex ?? -1) >= 0) {
            sourceTexture = Game1.content.Load<Texture2D>(found.Texture);
            sourceRect = new(((int)found.SpriteIndex * 8) % sourceTexture.Width,
                    (((int)found.SpriteIndex * 8) / sourceTexture.Width) * 16,
                    8, 16);
        }
        else {
            sourceTexture = (coloredText ? SpriteText.coloredTexture : SpriteText.spriteTexture);
            sourceRect = (Rectangle)Method_getSourceRectForChar.Invoke(null, new object[] {c, junimoText});
        }

        if ((found?.Baseline ?? -1) >= 0) {
            baselineOffset = BaselineConvert((int)found.Baseline);
        }
        else {
            baselineOffset = BaselineConvert(GetDefaultBaselineForChar(c, junimoText));
        }
    }

    internal static float BaselineConvert(int ypos)
    {
        return -4f + ypos;
    }

    internal static int GetDefaultBaselineForChar(char c, bool junimoText)
    {
        if (junimoText) {
            return 3;
        }
        switch (c) {
        case 'ß':
            return 0;
        case 'Ą':
        case 'Ę':
            return 1;
        case 'Ç':
        case 'Ş':
            return 2;
        }
        return char.IsUpper(c) ? 0 : 3;
    }

    /*
     * this and the next one are just to make the transpiler easier to write lol
     */
    internal static void ShiftDrawStart(ref float x, string s, int i)
    {
        x -= (SpriteText.FontPixelZoom * -1 * SpriteText.getWidthOffsetForChar(s[i]));
    }

    internal static void ShiftDrawEnd(ref float x, string s, int i)
    {
        x += (SpriteText.FontPixelZoom * (8f + SpriteText.getWidthOffsetForChar(s[i])));
    }


    internal static void PatchMethod(Harmony harmony, Type t, string name,
            Type[] argTypes, string patch)
    {
        string[] parts = patch.Split("_");
        string last = parts[parts.Length-1];
        if (last != "Prefix" && last != "Postfix" && last != "Transpiler") {
            Log.Error($"Skipping patch method '{patch}': bad type '{last}'");
            return;
        }
        try {
            MethodInfo m;
            if (argTypes is null) {
                m = t.GetMethod(name,
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static);
            }
            else {
                m = t.GetMethod(name,
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static,
                        null, argTypes, null);
            }
            HarmonyMethod func = new(typeof(Patches), patch);
            if (last == "Prefix") {
                harmony.Patch(original: m, prefix: func);
            }
            else if (last == "Postfix") {
                harmony.Patch(original: m, postfix: func);
            }
            else if (last == "Transpiler") {
                harmony.Patch(original: m, transpiler: func);
            }
            Log.Trace($"Patched method '{t.Name}.{m.Name}' ({last})");
        }
        catch (Exception e) {
            Log.Error($"Patch failed ({patch}): {e}");
        }
    }
}
