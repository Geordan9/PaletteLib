using System.Drawing;

namespace PaletteLib.Core.Adobe;

public class ASE
{
    public static byte[] MagicBytes { get; } = {0x41, 0x53, 0x45, 0x46};

    public int BlockCount { get; set; }

    public Color[] Colors { get; set; }
}