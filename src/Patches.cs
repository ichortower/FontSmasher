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
        //PatchMethod(harmony, typeof(ClassToPatch),
                //nameof(ClassToPatch.Method),
                //new[]{typeof(arg1), ...}, // or null
                //nameof(Patches.MethodToApply));
        PatchMethod(harmony, typeof(StardewValley.BellsAndWhistles.SpriteText),
                nameof(StardewValley.BellsAndWhistles.SpriteText.getWidthOffsetForChar),
                null,
                nameof(Patches.SpriteText_getWidthOffsetForChar_Postfix));
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
            ref SpriteFont spriteFont)
    {
        int i = -1;
        if (System.Object.ReferenceEquals(spriteFont, Game1.dialogueFont)) {
            i = 0;
        }
        else if (System.Object.ReferenceEquals(spriteFont, Game1.smallFont)) {
            i = 1;
        }
        else if (System.Object.ReferenceEquals(spriteFont, Game1.tinyFont)) {
            i = 2;
        }
        _ = Glyphs.Data;
        if (i == 0) {
            spriteFont = Game1.dialogueFont;
        }
        else if (i == 1) {
            spriteFont = Game1.smallFont;
        }
        else if (i == 2) {
            spriteFont = Game1.tinyFont;
        }
    }

    internal static void SpriteText_getWidthOffsetForChar_Postfix(
            char c, ref int __result)
    {
        if (Glyphs.Data.TryGetValue(c, out var val) &&
                val.BothSidesWidthOffset is not null) {
            __result = (int)val.BothSidesWidthOffset;
        }
        /*
        if (!Main.Config.ReplaceVanillaDialogueFont) {
            return;
        }
        // covered by base function:
        // -1: !¡ş
        // -2: .,
        //
        // the second line of each of these strings is for cyrillic letters. the "duplicates"
        // appearing there are distinct from the identical latin ones
        string minus1 = "aàáâãäåąbcçćdeèéêëęfgğhknñńoòóôõöőpqrsśtuùúûüűvxyýÿzźżðþ0123456789 ?¿()|:;-\"/\\" +
                "абвгґдеёзийклнопрстуўхчъьэєя";
        string minus2 = "iìíîïıjlł'’ᵃᵒ" +
                "ії";
        if (minus1.IndexOf(c) != -1) {
            __result = -1;
        }
        else if (minus2.IndexOf(c) != -1) {
            __result = -2;
        }
        */
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

    internal static IEnumerable<CodeInstruction> SpriteText_drawString_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        LocalBuilder sourceTexture = generator.DeclareLocal(typeof(Texture2D));
        MethodInfo GetSourceForChar = typeof(Patches).GetMethod(nameof(Patches.GetSourceForChar),
                BindingFlags.NonPublic | BindingFlags.Static);
        LocalBuilder yOffset = generator.DeclareLocal(typeof(float));
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
        // ours returns more values (via out parameters), so get those sorted out afterward as well
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
        // preserve ldarg.0 for the eventual draw call
        .Advance(1)
        // remove texture selection and use our baseline offset and texture instead
        .RemoveInstructions(5)
        .InsertAndAdvance(
            new (OpCodes.Ldloca_S, 15),
            new (OpCodes.Ldloc_S, yOffset),
            new (OpCodes.Stfld, Vector2YField),
            new (OpCodes.Ldloc_S, sourceTexture));

        return cm.InstructionEnumeration();
    }


    /*
     */
    internal static void GetSourceForChar(char c, bool coloredText, bool junimoText,
            out Texture2D sourceTexture, out Rectangle sourceRect, out float baselineOffset)
    {
        GridGlyph found = null;
        if (Glyphs.Data.TryGetValue(c, out var entry)) {
            if (coloredText && entry.DialogueColored is not null) {
                found = entry.DialogueColored;
            }
            else if (junimoText && entry.Junimo is not null) {
                found = entry.Junimo;
            }
            else if (entry.Dialogue is not null) {
                found = entry.Dialogue;
            }
        }
        
        if (found?.Texture is not null && (found?.SpriteIndex ?? -1) >= 0) {
            sourceTexture = Game1.content.Load<Texture2D>(found.Texture);
            sourceRect = new((found.SpriteIndex * 8) % sourceTexture.Width,
                    ((found.SpriteIndex * 8) / sourceTexture.Width) * 16,
                    8, 16);
        }
        else {
            sourceTexture = (coloredText ? SpriteText.coloredTexture : SpriteText.spriteTexture);
            sourceRect = (Rectangle)Method_getSourceRectForChar.Invoke(null, new object[] {c, junimoText});
        }
        if ((found?.BaselineOffset ?? -1) >= 0) {
            baselineOffset = poffset2f(found.BaselineOffset);
        }
        else {
            baselineOffset = poffset2f(GetDefaultBaselineOffsetForChar(c, junimoText));
        }
    }

    internal static float poffset2f(int ypos)
    {
        return -4f + ypos;
    }

    internal static int GetDefaultBaselineOffsetForChar(char c, bool junimoText)
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
