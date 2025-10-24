using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ichortower.FontSmasher;

/*
 */
internal sealed class BoxPacker
{
    /*
     */
    public static List<PackItem> Pack(List<PackItem> input, int width, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        // b - a is descending order (i have to write this down because it's impossible to remember)
        input.Sort((a, b) => {
            int hd = b.Bounds.Height - a.Bounds.Height;
            int wd = b.Bounds.Width - a.Bounds.Width;
            return hd != 0 ? hd : wd;
        });
        // start with a square; increase height by width and try again if no room.
        // generally expect caller (me!) to provide a sane width value
        for (int i = 0; ; ++i) {
            List<PackItem> ret = new();
            List<Rectangle> freeSpaces = new() { new(0, 0, width, width*(i+1)) };
            foreach (PackItem item in input) {
                Rectangle found = ClaimSpace(item.Bounds.Scale(item.OutputScale),
                        ref freeSpaces);
                if (found == Rectangle.Empty) {
                    // couldn't pack; try a bigger texture
                    bounds = Rectangle.Empty;
                    break;
                }
                ret.Add(new PackItem() {
                    Character = item.Character,
                    Texture = item.Texture,
                    Bounds = found,
                    OutputScale = item.OutputScale,
                    OriginalRect = item.Bounds,
                });
                bounds.Width = Math.Max(bounds.Width, found.Right);
                bounds.Height = Math.Max(bounds.Height, found.Bottom);
            }

            if (bounds != Rectangle.Empty) {
                return ret;
            }
        }
    }

    private static Rectangle ClaimSpace(Rectangle item, ref List<Rectangle> spaces)
    {
        Rectangle ret;
        // reminder to keep smaller rects at the front of the list so they are found first
        // when space runs low
        for (int i = 0; i < spaces.Count; ++i) {
            if (item.Width > spaces[i].Width || item.Height > spaces[i].Height) {
                continue;
            }
            ret = new(spaces[i].X, spaces[i].Y, item.Width, item.Height);
            // exactly fits the space: just remove it
            if (item.Width == spaces[i].Width && item.Height == spaces[i].Height) {
                spaces.RemoveAt(i);
                return ret;
            }
            // fits in exactly one dimension: trim but don't remove
            // the temp assigners are required because xna Rectangle is a struct and
            // something something intermediate copies
            if (item.Width == spaces[i].Width) {
                Rectangle temp = spaces[i];
                temp.Y = ret.Bottom;
                temp.Height -= item.Height;
                spaces[i] = temp;
                return ret;
            }
            if (item.Height == spaces[i].Height) {
                Rectangle temp = spaces[i];
                temp.X = ret.Right;
                temp.Width -= item.Width;
                spaces[i] = temp;
                return ret;
            }
            // standard result: smaller in both dimensions
            // since we want to pack across rows when possible, always split so the small
            // box is to the right: short and wide
            // insert the small one second at the same index, so it comes first next time
            int freeW = spaces[i].Width - item.Width;
            int freeH = spaces[i].Height - item.Height;
            Rectangle smallBox;
            Rectangle bigBox;
            //if (freeW > freeH) {
                //bigBox = new(ret.Right, spaces[i].Y, freeW, spaces[i].Height);
                //smallBox = new(spaces[i].X, ret.Bottom, ret.Width, freeH);
            //}
            //else {
                bigBox = new(spaces[i].X, ret.Bottom, spaces[i].Width, freeH);
                smallBox = new(ret.Right, spaces[i].Y, freeW, ret.Height);
            //}
            spaces.RemoveAt(i);
            spaces.Insert(i, bigBox);
            spaces.Insert(i, smallBox);
            return ret;
        }
        return Rectangle.Empty;
    }
}

internal class PackItem
{
    public char Character;
    public string Texture;
    public Rectangle Bounds;
    public int OutputScale;
    public Rectangle OriginalRect;
}
