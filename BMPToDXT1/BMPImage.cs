using System;
using System.IO;

namespace BMPDDSConverter
{
    /// <summary>
    /// Contains all constants required for file validation, and creation of new BMP file 
    /// </summary>
    public struct BMPConst
    {
        public const UInt16 MAGIC_IDENTIFIER = 0x4D42;      //BMP file signature
        public const UInt32 OFFSET = 54;                    //Header,where the pixel array (bitmap data) can be found, 14 bytes + InfoHeader 40 bytes = 54 bytes
        public const UInt32 INFOHEADER_SIZE = 40;           //Info header size must be 40
        public const UInt16 PLANES = 1;                     //Planes in the image
        public const UInt16 BITS = 24;                      //24 bits per pixel
        public const UInt32 COMPRESSION = 0;                //Compression = none

        public const UInt16 ZERO_2BYTES = 0;
        public const UInt32 ZERO_4BYTES = 0;
    };

    /// <summary>
    /// Holds BMP structure with reading and saving functionality
    /// https://en.wikipedia.org/wiki/BMP_file_format
    /// </summary>
    public class BMPImage
    {
        #region Structs
        private struct Header
        {
            public UInt16 Type;             //Type of file in ASCII, 2 bytes
            public UInt32 Size;             //Size of file in bytes, 4 bytes  
            public UInt16 Reserved1;        //Reserved, keep 0, 2 bytes   
            public UInt16 Reserved2;        //Reserved, keep 0, 2 bytes
            public UInt32 OffsetImageData;  //Starting address of image data, 4 bytes

            /// <summary>
            /// Parses the header of BMP file
            /// </summary>
            /// <param name="br"></param>
            /// <returns>true - if paring is successful, and data is correct</returns>
            public bool Parse(BinaryReader br)
            {
                Type = br.ReadUInt16();
                if(Type!= BMPConst.MAGIC_IDENTIFIER)
                {
                    Console.WriteLine("File is not in BMP format");
                    return false;
                }

                Size = br.ReadUInt32();
                if(Size< BMPConst.OFFSET)
                {
                    Console.WriteLine("File is too small, not containing header");
                    return false;
                }
                Reserved1 = br.ReadUInt16();
                Reserved2 = br.ReadUInt16();
                OffsetImageData = br.ReadUInt32();

                return true;
            }
        };

        private struct DIBHeader
        {
          
            public UInt32 Size;             // Size of header (40 bytes)
            public UInt32 Width;            // Image width in pixels 
            public UInt32 Height;           // Image height in pixels
            public UInt16 Planes;           // Number of color planes, Must be 1  
            public UInt16 BitCount;         // Bits per pixels.. 1..4..8..16..32 
            public UInt32 Compression;      // 0 No compression   
            public UInt32 SizeImage;        // Image size, may be zero for uncompressed 
            public UInt32 XPelsPerMeter;   // Horizontal resolution (pixel per meter, signed integer) 
            public UInt32 YPelsPerMeter;    //Vertical resolution(pixel per meter, signed integer)
            public UInt32 ClrUsed;          //Number of colors in the color palette, or 0 to default to 2n    
            public UInt32 ClrImportant;     //Number of important colors used, or 0 when every color is important; generally ignored

            /// <summary>
            ///  Parse the bitmap information header
            /// </summary>
            /// <param name="br"></param>
            public void Parse(BinaryReader br)
            {
                Size = br.ReadUInt32();
                Width = br.ReadUInt32();
                Height = br.ReadUInt32();
                Planes = br.ReadUInt16();
                BitCount = br.ReadUInt16();
                Compression = br.ReadUInt32();
                SizeImage = br.ReadUInt32();
                XPelsPerMeter = br.ReadUInt32();
                YPelsPerMeter = br.ReadUInt32();
                ClrUsed = br.ReadUInt32();
                ClrImportant = br.ReadUInt32();
            }

        };
        #endregion

        public UInt32 Height { get { return dibHeader.Height; } }
        public UInt32 Width { get { return dibHeader.Width; } }
        public UInt16 Type { get { return fileHeader.Type; } }
        public UInt32 FileSize { get { return fileHeader.Size; } }
        public string FileName { get { return fileName; } set { fileName = value; } }

        private RGBColor[,] imageData;
        private Header fileHeader;
        private DIBHeader dibHeader;
        private string fileName;
        private const string EXTENSION = ".bmp";

        public BMPImage()
        {

        }

        public BMPImage(UInt32 width, UInt32 height)
        {
            //Initialized empty BMP file
            dibHeader.Height = height;
            dibHeader.Width = width;
            imageData = new RGBColor[width, height];
        }
       
        public BMPImage(string fileName)
        {
            this.fileName = fileName;
            Read(fileName);
        }
        
        public void AddImageData(int x, int y, RGBColor rgb)
        {
            imageData[x, y] = rgb;
        }

        public RGBColor GetImageData(int x, int y)
        {
            return imageData[x, y];
        }
        /// <summary>
        /// Creates and saves bmp file
        /// </summary>
        /// <param name="fileName">name of file to be saved</param>
        public void Save(string fileName)
        {
            using (BinaryWriter b = new BinaryWriter(File.Create(fileName+ EXTENSION)))
            {
                //Writes header
                UInt32 imageDataSize = (UInt32)(Height * Width) * 3;
                UInt32 imageSize = imageDataSize + BMPConst.OFFSET;
                b.Write(BMPConst.MAGIC_IDENTIFIER);
                b.Write(imageSize);
                b.Write(BMPConst.ZERO_2BYTES);
                b.Write(BMPConst.ZERO_2BYTES);
                b.Write(BMPConst.OFFSET);

                //Writes DIBHeader
                b.Write(BMPConst.INFOHEADER_SIZE);
                b.Write(Width);
                b.Write(Height);
                b.Write(BMPConst.PLANES);
                b.Write(BMPConst.BITS);
                b.Write(BMPConst.COMPRESSION);
                b.Write(imageDataSize);
                b.Write(BMPConst.ZERO_4BYTES);
                b.Write(BMPConst.ZERO_4BYTES);
                b.Write(BMPConst.ZERO_4BYTES);
                b.Write(BMPConst.ZERO_4BYTES);

                //Writes data
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        b.Write((byte)imageData[x, y].B);
                        b.Write((byte)imageData[x, y].G);
                        b.Write((byte)imageData[x, y].R);
                    }
                }

                b.Close();
            }
        }

        /// <summary>
        /// Loads and reads BMP file
        /// </summary>
        /// <param name="fileName"></param>
        public void Read(string fileName)
        {
            byte[] rawData = File.ReadAllBytes(fileName+ EXTENSION);
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    //read header
                    if (fileHeader.Parse(br))
                    {
                        //read DIBHeader
                        dibHeader.Parse(br);

                        //read data
                        imageData = new RGBColor[Width, Height];
                        for (int y = 0; y < Height; y++)
                        {
                            for (int x = 0; x < Width; x++)
                            {
                                //Colors channels are stored in b,g,r order
                                byte b = br.ReadByte();
                                byte g = br.ReadByte();
                                byte r = br.ReadByte();
                                imageData[x, y] = new RGBColor(r, g, b);
                            }
                        }
                    }
                    else { Console.WriteLine("Wrong data"); }
                }

            }
        }
    }
}
