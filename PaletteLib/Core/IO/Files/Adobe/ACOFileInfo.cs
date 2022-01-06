using System.Drawing;
using System.IO;
using System.Text;
using PaletteLib.Core.Adobe;
using PaletteLib.Util;
using VFSILib.Common.Enum;
using VFSILib.Core.IO;

namespace PaletteLib.Core.IO.Files.Adobe;

public class ACOFileInfo : VirtualFileSystemInfo
{
    public ACOFileInfo(string path, short protocol = 1, bool preCheck = true) : base(path, preCheck)
    {
        GetColors(protocol);
    }

    public ACOFileInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent, short protocol = 1,
        bool preCheck = true) :
        base(path, length,
            offset, parent, preCheck)
    {
        GetColors(protocol);
    }

    public ACOFileInfo(MemoryStream memstream, string name = "Memory", short protocol = 1, bool preCheck = true) :
        base(memstream, name,
            preCheck)
    {
        GetColors(protocol);
    }

    public ACO ACOFile { get; } = new();

    public bool IsValidACO
    {
        get
        {
            using var reader = new EndiannessAwareBinaryReader(GetReadStream(), ByteOrder.BigEndian);
            var protocol = reader.ReadInt16();
            if (protocol != 1)
                return false;
            var colorRange = reader.ReadInt16();
            reader.BaseStream.Seek(colorRange * 10, SeekOrigin.Current);
            protocol = reader.ReadInt16();
            if (protocol != 2)
                return false;

            reader.Close();
            return true;
        }
    }

    public Color[] Colors
    {
        get => ACOFile.Colors;
        private set => ACOFile.Colors = value;
    }

    private void ReadACOColorData(EndiannessAwareBinaryReader reader)
    {
        var protocol = reader.ReadInt16();
        ACOFile.ColorRange = reader.ReadInt16();
        var colors = new Color[ACOFile.ColorRange];
        var colorNames = new string[ACOFile.ColorRange];
        for (var i = 0; i < ACOFile.ColorRange; i++)
        {
            var colorSpace = reader.ReadInt16();
            var w = reader.ReadInt16();
            var x = reader.ReadInt16();
            var y = reader.ReadInt16();
            var z = reader.ReadInt16();

            switch (colorSpace)
            {
                case 0:
                    colors[i] = Color.FromArgb(w & 0xFF, x & 0xFF, y & 0xFF);
                    break;
                case 1:
                    colors[i] = ColorTools.ConvertHsvToRgb(
                        (double) (w & 0xFF) / byte.MaxValue * 360,
                        (double) (x & 0xFF) / byte.MaxValue,
                        (double) (y & 0xFF) / byte.MaxValue);
                    break;
                case 2:
                    colors[i] = ColorTools.ConvertCMYKToRgb(
                        ushort.MaxValue - (ushort) w,
                        ushort.MaxValue - (ushort) x,
                        ushort.MaxValue - (ushort) y,
                        ushort.MaxValue - (ushort) z,
                        ushort.MaxValue);
                    break;
                case 7:
                    colors[i] = ColorTools.ConvertLabToRgb(w, x, y, 100);
                    break;
                case 8:
                    colors[i] = Color.FromArgb(w & 0xFF, w & 0xFF, w & 0xFF);
                    break;
            }

            if (protocol == 2)
            {
                reader.BaseStream.Seek(2, SeekOrigin.Current);
                var colorNameLength = reader.ReadInt16() * 2;
                colorNames[i] = Encoding.Unicode.GetString(reader.ReadBytes(colorNameLength)).Replace("\0", "");
            }
        }

        Colors = colors;
        ACOFile.ColorNames = colorNames;
    }

    private void GetColors(int protocol)
    {
        if (NoAccess)
            return;

        if (!Initialized) Initialize();

        if (!IsValidACO)
            return;

        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), ByteOrder.BigEndian);
        if (protocol > 0)
        {
            ReadACOColorData(reader);
            if (protocol == 2 && reader.BaseStream.Position < reader.BaseStream.Length &&
                reader.ReadInt16() == 2)
            {
                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                ReadACOColorData(reader);
            }
        }

        reader.Close();
    }
}