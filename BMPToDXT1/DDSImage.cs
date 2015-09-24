using System;
using System.Collections.Generic;
using System.IO;

namespace BMPDDSConverter
{
    /// <summary>
    /// Contains all constants required for file validation, and creation of new BMP file 
    /// </summary>
    public struct DDSConst
    {
        public const UInt32 MAGIC_IDENTIFIER = 0x20534444;      // "DDS "
        public const UInt32 FLAGS = 0x1007;                     //DDSD_PIXELFORMAT 0x1000, DDSD_CAPS 0x1, DDSD_HEIGHT 0x2, DDSD_WIDTH 0x4
        public const UInt32 PIXELFORMAT_SIZE = 0x20;            // 32 bytes
        public const UInt32 PIXELFORMAT_FLAGS = 0x4;
        public const UInt32 PIXELFORMAT_FOUR_CC = 0x31545844;   // "DXT1"
        public const UInt32 CAPS = 0x1000;
        public const UInt32 ZERO_4BYTES = 0;
        public const UInt32 DDS_BLOCK_SIZE = 16;
        public const UInt32 SIZE = 0x7C;                        //124
    }

    /// <summary>
    /// Holds DDS structure with reading and saving functionality
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb943990(v=vs.85).aspx
    /// </summary>
    public class DDSImage
    {
        #region Structs
        /// <summary>
        /// BC1 compression block
        /// </summary>
        public struct TexelBlock
        {
            public UInt16 color0; //maxColor
            public UInt16 color1; //minColor
            public byte[] blocks; //2-bit colors blocks
        };

        private struct PixelFormat
        {
            public UInt32 Size;     //Structure size; set to 32 (bytes)
            public UInt32 Flags;
            public UInt32 FourCC;
            public UInt32 RgbBitCount;
            public UInt32 RBitMask;
            public UInt32 GBitMask;
            public UInt32 BBitMask;
            public UInt32 ABitMask;
        };
        private struct Header
        {
            public UInt32 Type;      //magic number 0x20534444 ('DDS')
            public UInt32 Size;      //size of structure, must be set to 124
            public UInt32 Flags;
            public UInt32 Height;
            public UInt32 Width;
            public UInt32 PitchOrLinearSize;
            public UInt32 Depth;
            public UInt32 MipMapCount;
            public UInt32[] Reserved;
            public PixelFormat Ddspf;
            public UInt32 Caps;
            public UInt32 Caps2;
            public UInt32 Caps3;
            public UInt32 Caps4;
            public UInt32 Reserved2;

            public bool Parse(BinaryReader br)
            {
                Type = br.ReadUInt32();
                if (Type != DDSConst.MAGIC_IDENTIFIER)
                {
                    Console.WriteLine("File is not in DDS format");
                    return false;
                }
                Size = br.ReadUInt32();

                if (Size < DDSConst.SIZE)
                {
                    Console.WriteLine("File to small, no header");
                    return false;
                }
                Flags = br.ReadUInt32();
                Height = br.ReadUInt32();
                Width = br.ReadUInt32();
                PitchOrLinearSize = br.ReadUInt32();
                Depth = br.ReadUInt32();
                MipMapCount = br.ReadUInt32();
                Reserved = new UInt32[11];
                for (int i = 0; i < 11; ++i)
                {
                    Reserved[i] = br.ReadUInt32();
                }
                ReadDDSPixelFormat(br);
                Caps = br.ReadUInt32();
                Caps2 = br.ReadUInt32();
                Caps3 = br.ReadUInt32();
                Caps4 = br.ReadUInt32();
                Reserved2 = br.ReadUInt32();
                return true;
            }

            private void ReadDDSPixelFormat(BinaryReader br)
            {
                Ddspf.Size = br.ReadUInt32();
                Ddspf.Flags = br.ReadUInt32();
                Ddspf.FourCC = br.ReadUInt32();
                Ddspf.RgbBitCount = br.ReadUInt32();
                Ddspf.RBitMask = br.ReadUInt32();
                Ddspf.GBitMask = br.ReadUInt32();
                Ddspf.BBitMask = br.ReadUInt32();
                Ddspf.ABitMask = br.ReadUInt32();
            }
        
        };
        #endregion

        public UInt32 Height { get { return fileHeader.Height; } }
        public UInt32 Width { get { return fileHeader.Width; } }
        public UInt32 Type { get { return fileHeader.Type; } }
        public UInt32 FileSize { get { return fileHeader.Size; } }
        public string FileName { get { return fileName; } set { fileName = value; } }
        public int ImageDataLength { get { return imageData.Count; } }

        private List<TexelBlock> imageData;
        private Header fileHeader;
        private string fileName;
        private const string EXTENSION = ".dds";

        public DDSImage()
        {
        }

        public DDSImage(UInt32 width, UInt32 height)
        {
            //Initialized empty DDS file
            fileHeader.Width = width;
            fileHeader.Height = height;
            imageData = new List<TexelBlock>();
        }

        public DDSImage(string fileName)
        {
            this.fileName = fileName;
            Read(fileName);
        }

        public void AddImageData(TexelBlock tb)
        {
            imageData.Add(tb);
        }

        public TexelBlock GetImageData(int index)
        {
            if(index<imageData.Count)
                return imageData[index];

            return new TexelBlock();
        }

        /// <summary>
        /// Creates and saves dds file
        /// </summary>
        /// <param name="fileName">name of file to be saved</param>
        public void Save(string fileName)
        {
            using (BinaryWriter br = new BinaryWriter(File.Create(fileName+ EXTENSION)))
            {
                //Writes header
                br.Write(DDSConst.MAGIC_IDENTIFIER);
                br.Write(DDSConst.SIZE);
                br.Write(DDSConst.FLAGS);
                br.Write(Height);
                br.Write(Width);

                for (int i = 0; i < 14; i++)
                {
                    br.Write(DDSConst.ZERO_4BYTES);
                }

                br.Write(DDSConst.PIXELFORMAT_SIZE);
                br.Write(DDSConst.PIXELFORMAT_FLAGS);
                br.Write(DDSConst.PIXELFORMAT_FOUR_CC);

                for (int i = 0; i < 5; i++)
                {
                    br.Write(DDSConst.ZERO_4BYTES);
                }

                for (int i = 0; i < 5; i++)
                {
                    br.Write(DDSConst.ZERO_4BYTES);
                }

                //writes data
                for (int i = 0; i < imageData.Count; i++)
                {
                    br.Write(imageData[i].color0);
                    br.Write(imageData[i].color1);
                    br.Write(imageData[i].blocks[0]);
                    br.Write(imageData[i].blocks[1]);
                    br.Write(imageData[i].blocks[2]);
                    br.Write(imageData[i].blocks[3]);
                }

                br.Close();
            }
        }

        /// <summary>
        /// Loads and reads DDS file
        /// </summary>
        /// <param name="fileName"></param>
        public void Read(string fileName)
        {
            byte[] rawData = File.ReadAllBytes(fileName+ EXTENSION);
            imageData = new List<TexelBlock>();
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    fileHeader.Parse(r);

                    TexelBlock t;
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        t = new TexelBlock();
                        t.color0 = r.ReadUInt16();
                        t.color1 = r.ReadUInt16();
                        t.blocks = new byte[4];
                        t.blocks[0] = r.ReadByte();
                        t.blocks[1] = r.ReadByte();
                        t.blocks[2] = r.ReadByte();
                        t.blocks[3] = r.ReadByte();
                        imageData.Add(t);
                    }
                }
            }
        }
    }
}
