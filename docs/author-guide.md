# Font Smasher - Author Guide

This document explains how to use this mod to edit and/or replace font glyphs
in Stardew Valley. For a complete real-world example, consult the sample pack
that came bundled with this mod.


## Contents

bleh


## Introduction

Font Smasher works by providing data assets for other mods to edit. At this
time, clients are expected to be Content Patcher content packs; a C#/SMAPI
interface may follow later.

Generally, the way to add or edit glyphs is to target the assets for the fonts
you want to change, and provide data and/or textures for each glyph in each
one. Once you've done that, simply use the glyphs directly in your mod's
strings, as if they were there all along. Let's begin!


## Bold Fonts

The **bold fonts** are used by the game in dialogue boxes, mail, and titles or
section headings in some menus. They are the simplest to patch at this time,
because they are more restrictive. To edit them, target this asset:

`ichortower.FontSmasher/BoldFont`


### Format

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Glyphs</code></td>
<td>string-&gt;model dictionary</td>
<td>

The only field present in this font (this is for two reasons: one, to harmonize
with [the SpriteFont assets](#sprite-fonts), and two, to allow myself room to
add font-wide fields later on). The string key should be a single character
which is the unicode code point for this glyph, but see [Caveats](#caveats) for
more about unique JSON keys.

The model value follows the data format outlined below this one.

</td>
</tr>

</table>

### Glyph Model Format

All fields are optional.

<table>

<tr>
<th>Field</th>
<th>Type</th>
<th>Purpose</th>
</tr>

<tr>
<td><code>Dialogue</code></td>
<td>object</td>
<td>

An object with three optional fields (see below). This tells Font Smasher what
to do to represent this glyph in the plain dialogue-box version of the bold
font (i.e. `LooseSprites/font_bold`).

</td>
</tr>

<tr>
<td><code>Colored</code></td>
<td>object</td>
<td>

An object just like `Dialogue`, above, except this one controls the glyph used
for color-tinted bold text, like in mail and secret notes (i.e.
`LooseSprites/font_colored`).

</td>
</tr>
<tr>
<td><code>Junimo</code></td>
<td>object</td>
<td>

An object just like `Dialogue`, above, except this one controls the glyph used
for regular dialogue when `junimoText` is active.

</td>
</tr>
</tr>
<tr>
<td><code>LeftRightPadding</code></td>
<td>int</td>
<td>

How many pixels on both sides of the glyph's 8x16 texture are unused, and
should therefore be trimmed when rendering (default `0`). Due to limitations at
this time, the padding must apply to both sides, so the only valid character
widths are 8 (0), 6 (1), 4 (2), and 2 (3). In vanilla, most glyphs have 0
padding.

</td>
</tr>
</table>


### Glyph Sub-Object Format

<table>
<tr>
<td><code>Texture</code></td>
<td>string</td>
<td>

A game asset to draw this glyph's image from. You will need to Load any custom
texture you want to reference here. If this is omitted from a newly-added
glyph, it will use the default vanilla texture.

</td>
</tr>
<tr>
<td><code>SpriteIndex</code></td>
<td>int</td>
<td>

The index on the Texture to use. Glyphs in the bold font are 8 by 16 pixels.

This is technically optional, but the default value (unspecified) means to use
the glyph's Unicode code point minus 32 (or one of the slew of hardcoded other
offsets to make font_bold line up), which is probably not what you want.

</td>
</tr>
<tr>
<td><code>Baseline</code></td>
<td>int</td>
<td>

How high off the bottom of the 8x16 glyph sprite the font's baseline is, in
pixels. If omitted, this is `0` for uppercase letters and `3` for lowercase
ones.

The vanilla glyphs put the bottom row of shadow on the baseline, and the actual
glyph is one pixel above it.

</td>
</tr>
</table>


### Example

For example, let's add a glyph to the bold fonts: `Ȁ`.

```js
{
  "Target": "{{ModId}}/NewGlyphs, {{ModId}}/NewGlyphs_Colored",
  "Action": "Load",
  "FromFile": "assets/{{TargetWithoutPath}}.png"
},
{
  "Target": "ichortower.FontSmasher/BoldFont",
  "Action": "EditData",
  "TargetField": ["Glyphs"],
  "Entries": {
    "Ȁ": {
      "Dialogue": {
        "Texture": "{{ModId}}/NewGlyphs",
        "SpriteIndex": 0
      },
      "Colored": {
        "Texture": "{{ModId}}/NewGlyphs_Colored",
        "SpriteIndex": 0
      },
      "Junimo": {
        "SpriteIndex": 257
      }
    }
  }
}
```

Here, we've used two new textures, each with Ȁ at the top-left, for the
Dialogue and Colored glyphs, and for junimo text we've given the index of the
junimo A in the font_bold sheet, since in vanilla all diacritic-marked junimo
letters are identical.

Alternately, let's edit the glyph `ğ`. As it is already present in font_bold,
we can edit the image assets and just change the data we need:

```js
{
  "Target": "LooseSprites/font_bold",
  "Action": "EditImage",
  "FromFile": "assets/g-breve.png"
  "ToArea": {
    "X": 56, "Y": 96, "Width": 8, "Height": 16
  }
},
{
  "Target": "LooseSprites/font_colored",
  "Action": "EditImage",
  "FromFile": "assets/g-breve-colored.png"
  "ToArea": {
    "X": 56, "Y": 96, "Width": 8, "Height": 16
  }
},
{
  "Target": "ichortower.FontSmasher/BoldFont",
  "Action": "EditData",
  "TargetField": ["Glyphs"],
  "Entries": {
    "ğ": {
      "Junimo": {
        "SpriteIndex": 263
      },
      "LeftRightPadding": 1
    }
  }
}
```

In this example, we've used a ğ glyph that is only 6 pixels wide, so we tell
Font Smasher to trim the whitespace when rendering. We also use the index for
`g`'s junimo glyph, so ğ will be visible if we display any junimo text.
