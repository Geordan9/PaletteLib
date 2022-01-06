using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VFSILib.Common.Enum;
using VFSILib.Core.IO;

namespace PaletteLib.Core.IO.Files;

public class JSACPALFileInfo : VirtualFileSystemInfo
{
    public JSACPALFileInfo(string path, bool preCheck = true) : base(path, preCheck)
    {
        GetPalette();
    }

    public JSACPALFileInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent,
        bool preCheck = true) :
        base(path, length,
            offset, parent, preCheck)
    {
        GetPalette();
    }

    public JSACPAL JSACPALFile { get; } = new();

    public Color[] Palette
    {
        get => JSACPALFile.Palette;
        private set => JSACPALFile.Palette = value;
    }

    public bool IsValidJSACPAL => MagicBytes.SequenceEqual(JSACPAL.MagicBytes);

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
        MagicBytes = reader.ReadBytes(0x8, ByteOrder.LittleEndian);
        if (!IsValidJSACPAL) return;

        JSACPALFile.Delimiter = reader.ReadInt16();
        var colorRanges = new short[2];
        colorRanges[0] = short.Parse(ReadString(reader), NumberStyles.HexNumber);
        colorRanges[1] = short.Parse(ReadString(reader));
        JSACPALFile.ColorRange = colorRanges.Min();
    }

    private void GetPalette()
    {
        if (NoAccess)
            return;

        if (!Initialized) Initialize();

        InitGetHeader();

        if (!IsValidJSACPAL)
            return;

        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), Endianness);
        reader.BaseStream.Seek(0x15, SeekOrigin.Current);

        var colors = new Color[JSACPALFile.ColorRange];

        for (var i = 0; i < colors.Length; i++)
        {
            var bytes = ReadString(reader).Split(' ').Select(c => byte.Parse(c)).ToArray();
            colors[i] = Color.FromArgb(bytes[0], bytes[1], bytes[2]);
        }

        JSACPALFile.Palette = colors;

        reader.Close();
    }

    private string ReadString(EndiannessAwareBinaryReader reader)
    {
        var firstByte = (byte) JSACPALFile.Delimiter;
        var secondByte = (byte) (JSACPALFile.Delimiter >> 8);
        var byteList = new List<byte>();
        do
        {
            byteList.Add(reader.ReadByte());
        } while (reader.BaseStream.Position < reader.BaseStream.Length &&
                 !(byteList.Count > 1 && byteList[byteList.Count - 1] == secondByte &&
                   byteList[byteList.Count - 2] == firstByte));

        byteList.RemoveRange(byteList.Count - 2, 2);

        return Encoding.ASCII.GetString(byteList.ToArray());
    }
}