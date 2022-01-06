using System.Drawing;

namespace PaletteLib.Core.Adobe;

public class ACO
{
    public short ColorRange { get; set; }

    public string[] ColorNames { get; set; }

    public Color[] Colors { get; set; }
}