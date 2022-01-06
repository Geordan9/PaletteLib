using System.Drawing;
using System.IO;
using VFSILib.Core.IO;

namespace PaletteLib.Util;

public static class PaletteTools
{
    public static readonly string[] supportedPaletteExtensions =
    {
        ".act", ".aco", ".ase", ".pal"
    };

    public static Color[] ReadGenericPAL(byte[] palBytes)
    {
        var colorRange = palBytes.Length / 4;
        var colors = new Color[colorRange];

        using (var reader = new EndiannessAwareBinaryReader(new MemoryStream(palBytes)))
        {
            for (var i = 0; i < colorRange; i++)
            {
                var bytes = reader.ReadBytes(4);
                colors[i] = Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
            }
        }

        return colors;
    }
}