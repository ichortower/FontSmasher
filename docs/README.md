# Font Smasher

This Stardew Valley mod is a framework for mod authors to use to replace or
extend the vanilla game fonts in an author-facing way, intended so that
modmakers can more easily ensure any additional glyphs they require are present
and supported.


## Goal (or: Why?)

First of all, the vanilla fonts aren't amenable to extension. Consider the bold
(dialogue) font:

- Relies on integer sprite indexes on a single shared texture, so is prone to
  collisions
- Uses a character's Unicode code point as an index, except when it has
  hardcoded exceptions, so some collisions are already baked in

And consider the sprite (interface) fonts:

- No ability to edit or extend, only to replace wholesale using an XNB
- Typically loaded before Content Patcher comes online and kept in cache for
  the whole session, so difficult to edit

Second, while [Font
Settings](https://www.nexusmods.com/stardewvalley/mods/12467) is a perfectly
cromulent mod and solves some of the same problems as Font Smasher (lack of
specific glyphs and/or desire to use different letterforms), it does so in a
user-facing way: it is fully controlled by the user and is opaque to mod
authors, so if a content modder wants to use extra diacritics or other special
characters not supported by the base game, the only recourse available is to
ask users to configure a font that includes the desired glyphs. Not only does
this add steps, it leaves the user and the modder unable to use the default
fonts, if they happen to like them.

Font Smasher's purpose is to address both of these issues. It allows modders to
add glyphs to and/or edit glyphs in the base fonts (or [go ham and replace them
all](https://github.com/ichortower/CobaltSans)), so they can set a dependency
on a particular font mod that provides the glyphs, or even include the Font
Smasher data directly in their own mod and eliminate the difficulty altogether.

Speaking of Font Settings...


## Compatibility

This mod almost certainly isn't compatible with Font Settings, since (I
presume) that mod reimplements font rendering in most/all situations and
probably negates or interferes with the changes I had to make to font
rendering.

In addition, right now this mod only partially supports Chinese, Japanese, and
Korean: the BmFont type used for those languages' dialogue fonts is not
supported, but they also use SpriteFonts for SpriteFont1 and SmallFont, and
those do work. However, their default sprite fonts are very different from the
latin ones, so it's probably better to treat them separately.


## How to Use

As a user, you will need [SMAPI 4.1.10+](https://smapi.io) and [Content
Patcher](https://github.com/Pathoschild/StardewMods/tree/stable/ContentPatcher).
Install this mod like any other, by unzipping it into your Mods folder, and let
the mods that require it do their work.

This mod comes bundled with a sample content pack (`FontSmasherSamplePack`)
which is intended for mod authors. If you don't need it, it is safe to delete.

As a mod author, this framework provides data assets which your mod should edit
in order to give it information about the glyphs you want to add or change. At
this time, you are expected to use Content Patcher for this. I may add a C# API
and/or better support for SMAPI's content API in the future, but I suspect
using Content Patcher will suffice for almost everyone.

For details about the data assets and how to use them, see the [author
guide](author-guide.md).


## Special Thanks

- Abagaianye in particular, for the snipe.
- Everyone on the Stardew Valley and Stardew Modmakers' discords, in general.
