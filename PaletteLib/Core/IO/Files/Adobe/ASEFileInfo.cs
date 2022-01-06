using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using PaletteLib.Core.Adobe;
using PaletteLib.Util;
using VFSILib.Common.Enum;
using VFSILib.Core.IO;

namespace PaletteLib.Core.IO.Files.Adobe;

public class ASEFileInfo : VirtualFileSystemInfo
{
    public ASEFileInfo(string path, bool preCheck = true) : base(path, preCheck)
    {
        GetColors();
    }

    public ASEFileInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent,
        bool preCheck = true) :
        base(path, length,
            offset, parent, preCheck)
    {
        GetColors();
    }

    public ASEFileInfo(MemoryStream memstream, string name = "Memory", bool preCheck = true) : base(memstream, name,
        preCheck)
    {
        GetColors();
    }

    public ASE ASEFile { get; } = new();

    public bool IsValidASE => MagicBytes.SequenceEqual(ASE.MagicBytes);

    public Color[] Colors
    {
        get => ASEFile.Colors;
        private set => ASEFile.Colors = value;
    }

    private void ReadHeaderInfo()
    {
        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), Endianness);
        MagicBytes = reader.ReadBytes(4, ByteOrder.LittleEndian);
        reader.BaseStream.Seek(4, SeekOrigin.Current);
        ASEFile.BlockCount = reader.ReadInt32();
        reader.Close();
    }

    private void GetColors()
    {
        if (NoAccess)
            return;

        if (!Initialized) Initialize();

        Endianness = ByteOrder.BigEndian;

        ReadHeaderInfo();

        if (!IsValidASE)
            return;

        using var reader = new EndiannessAwareBinaryReader(GetReadStream(), Endianness);
        reader.BaseStream.Seek(12, SeekOrigin.Current);
        var colorList = new List<Color>();
        for (var i = 0; i < ASEFile.BlockCount; i++)
        {
            var blockType = reader.ReadUInt16();
            //var blockLength = reader.ReadInt32();
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            if (blockType != 0xC002)
            {
                var blockNameLength = reader.ReadInt16() * 2;
                //var blockName = Encoding.Unicode.GetString(reader.ReadBytes(blockNameLength)).Replace("\0", "");
                reader.BaseStream.Seek(blockNameLength, SeekOrigin.Current);
                if (blockType != 0xC001)
                {
                    var colorSpace = Encoding.ASCII.GetString(reader.ReadBytes(4, ByteOrder.LittleEndian));
                    Color color;
                    switch (colorSpace)
                    {
                        case "CMYK":
                            color = ColorTools.ConvertCMYKToRgb(reader.ReadSingle(), reader.ReadSingle(),
                                reader.ReadSingle(), reader.ReadSingle());
                            break;
                        case "Lab ":
                            color = ColorTools.ConvertLabToRgb(reader.ReadSingle(), reader.ReadSingle(),
                                reader.ReadSingle());
                            break;
                        case "Gray":
                            var val = reader.ReadSingle();
                            color = ColorTools.FromArgb(val, val, val);
                            break;
                        default:
                            color = ColorTools.FromArgb(reader.ReadSingle(), reader.ReadSingle(),
                                reader.ReadSingle());
                            break;
                    }

                    //var colorMode = reader.ReadInt16();
                    reader.BaseStream.Seek(2, SeekOrigin.Current);
                    colorList.Add(color);
                }
            }
        }

        Colors = colorList.ToArray();

        reader.Close();
    }
}