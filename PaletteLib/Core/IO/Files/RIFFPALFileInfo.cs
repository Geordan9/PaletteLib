using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using VFSILib.Common.Enum;
using VFSILib.Core.IO;

namespace PaletteLib.Core.IO.Files;

public class RIFFPALFileInfo : VirtualFileSystemInfo
{
    public RIFFPALFileInfo(string path, bool preCheck = true) : base(path, preCheck)
    {
        GetPalette();
    }

    public RIFFPALFileInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent,
        bool preCheck = true) :
        base(path, length,
            offset, parent, preCheck)
    {
        GetPalette();
    }

    public RIFFPAL RIFFPALFile { get; } = new();

    public Color[] Palette
    {
        get => RIFFPALFile.Palette;
        private set => RIFFPALFile.Palette = value;
    }

    public bool IsValidRIFFPAL => MagicBytes.SequenceEqual(RIFFPAL.MagicBytes);

    private void InitGetHeader()
    {
        var stream = GetReadStream();
        if (stream == null)
            return;
        using (stream)
        {
            if (FileLength < 0x16)
                return;

            try
            {
                using var reader = new EndiannessAwareBinaryReader(stream, Endianness);
                if (!EndiannessChecked)
                {
                    reader.BaseStream.Seek(4, SeekOrigin.Current);
                    if (Endianness == ByteOrder.LittleEndian)
                    {
                        var dataLength = reader.ReadInt32();
                        if (dataLength != reader.BaseStream.Length - 4) Endianness = ByteOrder.BigEndian;
                    }

                    EndiannessChecked = true;
                    reader.ChangeEndianness(Endianness);
                    reader.BaseStream.Seek(-8, SeekOrigin.Current);
                }

                ReadHeaderInfo(reader);
                reader.Close();
            }
            catch
            {
            }
        }
    }

    private void ReadHeaderInfo(EndiannessAwareBinaryReader reader)
    {
        MagicBytes = reader.ReadBytes(0x4, ByteOrder.LittleEndian);
        if (!IsValidRIFFPAL) return;

        RIFFPALFile.DataLength = reader.ReadInt32();
        var chunkName = Encoding.ASCII.GetString(reader.ReadBytes(8, ByteOrder.LittleEndian));
        if (chunkName != RIFFPAL.ChunkName) return;
        RIFFPALFile.ChunkLength = reader.ReadInt32();
        reader.BaseStream.Seek(0x2, SeekOrigin.Current);
        RIFFPALFile.ColorRange = reader.ReadInt16();
    }

    private void GetPalette()
    {
        if (NoAccess)
            return;

        if (!Initialized) Initialize();

        InitGetHeader();

        if (!IsValidRIFFPAL)
            return;

        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), ByteOrder.LittleEndian);
        reader.BaseStream.Seek(0x18, SeekOrigin.Current);

        var colors = new Color[RIFFPALFile.ColorRange];

        for (var i = 0; i < colors.Length; i++)
        {
            var bytes = reader.ReadBytes(4, ByteOrder.LittleEndian);
            colors[i] = Color.FromArgb(bytes[0], bytes[1], bytes[2]);
        }

        RIFFPALFile.Palette = colors;

        reader.Close();
    }
}