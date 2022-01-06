using System.Drawing;

namespace PaletteLib.Core;

public class RIFFPAL
{
    public static byte[] MagicBytes { get; } = {0x52, 0x49, 0x46, 0x46};

    public static string ChunkName { get; } = "PAL data";

    public int DataLength { get; set; }

    public int ChunkLength { get; set; }

    public short ColorRange { get; set; }

    public Color[] Palette { get; set; }
}