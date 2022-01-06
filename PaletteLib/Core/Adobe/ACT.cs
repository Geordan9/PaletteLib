using System.Drawing;

namespace PaletteLib.Core.Adobe;

public class ACT
{
    public short ColorRange { get; set; }

    public short AlphaColorIndex { get; set; } = -1;

    public Color[] Palette { get; set; }
}