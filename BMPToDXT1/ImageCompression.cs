using System;

namespace BMPDDSConverter
{
    public static class ImageCompression
    {
        public static BMPImage ConvertDDSToBMP(DDSImage dds)
        {
            //Creates new empty BMP file
            BMPImage bmp = new BMPImage((UInt32)dds.Width, (UInt32)dds.Height);

            //container for colors
            RGBColor[] colors = new RGBColor[4];
            int texelsX = (int)dds.Width / 4;
            int x, y = 0;
            //iterates through all data in DDS file
            for (int i = 0; i < dds.ImageDataLength; i++)
            {
                DDSImage.TexelBlock t = dds.GetImageData(i);
                colors = ReadMainColors(t);
                
                RGBColor color = new RGBColor(0,0,0);

                byte[] texelCol = new byte[4];
                for (int k=0;k<t.blocks.Length;k++)
                {
                   
                    texelCol[0] = (byte)(t.blocks[k] & 0x3);
                    texelCol[1] = (byte)((t.blocks[k] & 0xC) >> 2);
                    texelCol[2] = (byte)((t.blocks[k] & 0x30) >> 4);
                    texelCol[3] = (byte)((t.blocks[k] & 0xC0) >> 6);

                    for (int j = 0; j < 4; j++)
                    {
                        color = colors[texelCol[j]];

                        x = (i % texelsX) * 4 + j;
                        y = ((int)dds.Height - 1) - ((i / texelsX) * 4) - k;

                        bmp.AddImageData(x, y, color);
                    }
                }
            }

            bmp.Save(dds.FileName);
            return bmp;
        }

        private static RGBColor[] ReadMainColors(DDSImage.TexelBlock texel)
        {
            RGBColor[] colors = new RGBColor[4];
            RGBColor rgbColor0 = ImageUtils.ConvertRGB565ToRGB888(texel.color0);
            RGBColor rgbColor1 = ImageUtils.ConvertRGB565ToRGB888(texel.color1);

            RGBColor rgbColor2 = new RGBColor(
                (int)((2.0f / 3.0f) * rgbColor0.R + (1.0f / 3.0f) * rgbColor1.R),
                (int)((2.0f / 3.0f) * rgbColor0.G + (1.0f / 3.0f) * rgbColor1.G),
                (int)((2.0f / 3.0f) * rgbColor0.B + (1.0f / 3.0f) * rgbColor1.B));
            RGBColor rgbColor3 = new RGBColor(
                (int)((1.0f / 3.0f) * rgbColor0.R + (2.0f / 3.0f) * rgbColor1.R),
                (int)((1.0f / 3.0f) * rgbColor0.G + (2.0f / 3.0f) * rgbColor1.G),
                (int)((1.0f / 3.0f) * rgbColor0.B + (2.0f / 3.0f) * rgbColor1.B));


            colors[0] = rgbColor0;
            colors[1] = rgbColor1;
            colors[2] = rgbColor2;
            colors[3] = rgbColor3;

            return colors;
        }

        public static DDSImage ConvertBMPToDDS(BMPImage bmp)
        {
            // In this algorithm (0,0) is the lower left corner because BMP data starts from there
            DDSImage dds = new DDSImage(bmp.Width,bmp.Height);

            for (int y = 0; y< bmp.Height; y += 4)
            {
                for (int x = 0; x < bmp.Width; x += 4)
                {

                    RGBColor[] block = new RGBColor[DDSConst.DDS_BLOCK_SIZE];
                    for (int i = 0; i < block.Length; i++)
                    {
                        block[i] = bmp.GetImageData(x +(i%4) , (int)bmp.Height - 1 - (y + (i / 4)));
                    }
                    dds.AddImageData(CompressDataBlock(block));
                }
            }

            dds.Save(bmp.FileName);
            return dds;
         
        }

        private static DDSImage.TexelBlock CompressDataBlock(RGBColor[] block)
        {
            //Finds max and min color
            RGBColor maxColor = new RGBColor(0, 0, 0);
            RGBColor minColor = new RGBColor(255, 255, 255);

            float max = 0;
            float min = 255f;
            for (int i = 0; i < block.Length; i++)
            {
                //Note: Testing Luminance to determine max and min color, but having some artifacts on pic
                /*
                if(max<Math.Max(max, ImageUtils.ColorBrightness(block[i])))
                {
                    max = Math.Max(max, ImageUtils.ColorBrightness(block[i]));
                    maxColor = block[i];
                }
                if (min > Math.Min(min, ImageUtils.ColorBrightness(block[i])))
                {
                    min = Math.Min(min, ImageUtils.ColorBrightness(block[i]));
                    minColor = block[i];
                }*/
                maxColor = ImageUtils.Max(maxColor, block[i]);
                minColor = ImageUtils.Min(minColor, block[i]);
            }

            DDSImage.TexelBlock tb = new DDSImage.TexelBlock();
            tb.blocks = new byte[4];

            UInt16 color2 = 0;
            UInt16 color3 = 0;

            tb.color0 = ImageUtils.ConvertRGB888ToRGB565(maxColor);
            tb.color1 = ImageUtils.ConvertRGB888ToRGB565(minColor);

           //Interpolate color2 and color3	
            int B = (int)(((2.0f / 3.0f) * maxColor.B) + ((1.0f / 3.0f) * minColor.B));
            int G = (int)(((2.0f / 3.0f) * maxColor.G) + ((1.0f / 3.0f) * minColor.G));
            int R = (int)(((2.0f / 3.0f) * maxColor.R) + ((1.0f / 3.0f) * minColor.R));
            color2 = ImageUtils.ConvertRGB888ToRGB565(new RGBColor(R, G, B));

            B = (int)(((1.0f / 3.0f) * maxColor.B) + ((2.0f / 3.0f) * minColor.B));
            G = (int)(((1.0f / 3.0f) * maxColor.G) + ((2.0f / 3.0f) * minColor.G));
            R = (int)(((1.0f / 3.0f) * maxColor.R) + ((2.0f / 3.0f) * minColor.R));
            color3 = ImageUtils.ConvertRGB888ToRGB565(new RGBColor(R, G, B));
  
            float[] distances = new float[4];
            int colorTableColIndex = 0;
            int colorTableRowIndex = 0;
            int colorTableRow = 0;

            //iterates through 16 block of colors 
            for (int x = 0; x < block.Length; x++)
            {
                UInt16 rgb565 = ImageUtils.ConvertRGB888ToRGB565(block[x]);
              
                //Euclidean distance between the two colors, no need to sqrt for comparison
                distances[0] = (float)Math.Pow(tb.color0 - rgb565, 2f);
                distances[1] = (float)Math.Pow(tb.color1 - rgb565, 2f);
                distances[2] = (float)Math.Pow(color2 - rgb565, 2f);
                distances[3] = (float)Math.Pow(color3 - rgb565, 2f);

                //Find minimum distance from calculated values
                float minDistance =  distances[0];
                int indexBlock = 0;

                for (int i = 1; i < 4; i++)
                {
                    if (distances[i] < minDistance)
                    {
                        minDistance = distances[i];
                        indexBlock = i;
                    }
                }
             
                //Write the index value to correct position in this color table row
                switch (colorTableColIndex)
                {
                    case 0:
                        colorTableRow = indexBlock;
                        break;
                    case 1:
                        colorTableRow |= (indexBlock << 2);
                        break;
                    case 2:
                        colorTableRow |= (indexBlock << 4);
                        break;
                    case 3:
                        colorTableRow |= (indexBlock << 6);
                        break;
                }

                colorTableColIndex++;

                //If all four values have been written to this color table row,
                //Save it to texel block structure and start getting data from next row
                if (colorTableColIndex == 4)
                {
                    tb.blocks[colorTableRowIndex] = (byte)colorTableRow;
                    colorTableRowIndex++;
                    colorTableColIndex = 0;
                }
            }
            return tb;
        }

     
    }
}
