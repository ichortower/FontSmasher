# Font Smasher

This Stardew Valley mod is a framework for mod authors to use to replace or
extend the vanilla game fonts in an author-facing way, intended so that
modmakers can more easily ensure any additional glyphs they require are present
and supported.


## Goal (or: Why?)

[Font Settings](https://www.nexusmods.com/stardewvalley/mods/12467) is a
perfectly cromulent mod that solves the same problems as Font Smasher, but in a
user-facing way: it is fully controlled by the user and is opaque to mod
authors, so if a content modder wants to use extra diacritics or other special
characters not supported by the base game, the only recourse available is to
ask users to configure a font that includes the desired glyphs. Not only is
this prone to error, it leaves the user and the modder unable to use the
default fonts, if they happen to like them.

Font Smasher's purpose is to allow modders to add the glyphs they need and/or
edit ones in the base fonts (or [replace them
entirely](https://github.com/ichortower/MerchantSans)), so the author can set a
dependency on a particular font mod that provides the glyphs, or even include
the Font Smasher data directly in their own mod and eliminate the difficulty
altogether.


## How to Use

As a user, you will need [SMAPI 4.1.10+](https://smapi.io) and [Content
Patcher](https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher).
Install this mod like any other, by unzipping it into your Mods folder, and let
the mods that require it do their work.

This mod comes bundled with a sample content pack (`FontSmasherSamplePack`),
which is intended for mod authors. If you don't need it, it is safe to delete.

As a mod author, this framework provides data assets which your mod should edit
in order to give it information about the glyphs you want to add or change. At
this time, you are expected to use Content Patcher for this. I may add a C# API
or better support for SMAPI's content API in the future, but I suspect using
Content Patcher will suffice for almost everyone.

For details about the data assets and how to use them, see the [author
guide](author-guide.md).


## Special Thanks

Abagaianye, for the snipe.
