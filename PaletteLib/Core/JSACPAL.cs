using System.Drawing;

namespace PaletteLib.Core;

public class JSACPAL
{
    public static byte[] MagicBytes { get; } = {0x4A, 0x41, 0x53, 0x43, 0x2D, 0x50, 0x41, 0x4C};

    public short Delimiter { get; set; }

    public short ColorRange { get; set; }

    public Color[] Palette { get; set; }
}