using System.Drawing;
using System.IO;
using PaletteLib.Core.Adobe;
using VFSILib.Common.Enum;
using VFSILib.Core.IO;

namespace PaletteLib.Core.IO.Files.Adobe;

public class ACTFileInfo : VirtualFileSystemInfo
{
    public ACTFileInfo(string path, bool preCheck = true) : base(path, preCheck)
    {
        Endianness = ByteOrder.BigEndian;
        GetPalette();
    }

    public ACTFileInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent,
        bool preCheck = true) :
        base(path, length,
            offset, parent, preCheck)
    {
        Endianness = ByteOrder.BigEndian;
        GetPalette();
    }

    public ACTFileInfo(MemoryStream memstream, string name = "Memory", bool preCheck = true) : base(memstream, name,
        preCheck)
    {
        Endianness = ByteOrder.BigEndian;
        GetPalette();
    }

    public ACTFileInfo(Color[] colors, ByteOrder endianness = ByteOrder.BigEndian) : base(
        new MemoryStream(new byte[0]))
    {
        Endianness = endianness;
        CreateACT(colors);
    }

    public ACT ACTFile { get; } = new();

    public Color[] Palette
    {
        get => ACTFile.Palette;
        private set => ACTFile.Palette = value;
    }

    private void CheckEndianness(long pos, short colorRange)
    {
        if (Endianness == ByteOrder.BigEndian)
            if (colorRange * 3 != pos)
                Endianness = ByteOrder.LittleEndian;
        EndiannessChecked = true;
    }

    private void InitGetFooter()
    {
        var stream = GetReadStream();
        if (stream == null)
            return;
        using (stream)
        {
            try
            {
                using var reader = new EndiannessAwareBinaryReader(stream, Endianness);
                if (!EndiannessChecked && reader.BaseStream.Length % 3 != 0)
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.End);
                    CheckEndianness(reader.BaseStream.Position, reader.ReadInt16());
                    reader.ChangeEndianness(Endianness);
                    reader.BaseStream.Position = 0;
                }

                ReadFooterInfo(reader);
                reader.Close();
            }
            catch
            {
            }
        }
    }

    private void ReadFooterInfo(EndiannessAwareBinaryReader reader)
    {
        reader.BaseStream.Seek(-4, SeekOrigin.End);
        ACTFile.ColorRange = reader.ReadInt16();
        ACTFile.AlphaColorIndex = reader.ReadInt16();
        if ((ulong) (ACTFile.ColorRange * 3 + 4) != FileLength)
            ACTFile.ColorRange = ACTFile.AlphaColorIndex = -1;
    }

    private void GetPalette()
    {
        if (NoAccess)
            return;

        if (!Initialized) Initialize();

        InitGetFooter();

        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), Endianness);
        var colors = new Color[ACTFile.ColorRange == -1 ? 256 : ACTFile.ColorRange];

        for (var i = 0; i < colors.Length; i++)
        {
            var bytes = reader.ReadBytes(3);
            colors[i] = Color.FromArgb(bytes[2], bytes[1], bytes[0]);
        }

        ACTFile.Palette = colors;

        reader.Close();
    }

    private void CreateACT(Color[] colors, bool useFooter = false)
    {
        if (colors == null || colors.Length == 0)
            return;

        Palette = colors;
        ACTFile.ColorRange = (short) Palette.Length;
        for (short i = 0; i < colors.Length; i++)
            if (colors[i].A > 0)
            {
                ACTFile.AlphaColorIndex = i;
                break;
            }

        FileLength = (ulong) (ACTFile.ColorRange * 3 + (useFooter ? 4 : 0));
        VFSIBytes = new byte[FileLength];
        using var writer = new EndiannessAwareBinaryWriter(new MemoryStream(VFSIBytes), Endianness);
        foreach (var color in Palette) writer.Write(new[] {color.B, color.G, color.R});

        if (useFooter)
        {
            writer.Write(ACTFile.ColorRange);
            writer.Write(ACTFile.AlphaColorIndex);
        }

        writer.Close();
    }
}