using System;

namespace BMPDDSConverter
{
    /// <summary>
    /// RGB color structure
    /// </summary>
    public struct RGBColor
    {
        public int R { get { return r; } }
        public int G { get { return g; } }
        public int B { get { return b; } }

        private int r;
        private int g;
        private int b;

        public RGBColor(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public override string ToString()
        {
            return "R- " + R + ", G- " + G + ", B- " + B;
        }
    }

    /// <summary>
    /// Help class for image processing
    /// </summary>
    public static class ImageUtils
    {

        public static float ColorBrightness(RGBColor rbg)
        {
            return 0.2126f * rbg.R + 0.7152f * rbg.G + 0.0722f * rbg.B;
        }
        /// <summary>
        /// Converts RGB888 (3 bytes) to RGB565 (2 bytes)
        /// </summary>
        /// <param name="rgb">RGB888</param>
        /// <returns>RGB565</returns>
        public static UInt16 ConvertRGB888ToRGB565(RGBColor rgb)
        { 
            UInt16 b = (UInt16)((byte)(rgb.B >> 3) & 0x001F);
            UInt16 g = (UInt16)(((byte)(rgb.G >> 2) << 5) & 0x07E0);
            UInt16 r = (UInt16)(((byte)(rgb.R >> 3) << 11) & 0xF800);
            return (UInt16)(b | g | r);
        }

        /// <summary>
        /// Converts RGB565 (2 bytes) to RGB888 (3 bytes)
        /// </summary>
        /// <param name="rgb565">RGB565</param>
        /// <returns>RGB888</returns>
        public static RGBColor ConvertRGB565ToRGB888(UInt16 rgb565)
        {
            int b = ((rgb565 & 0x001F));
            b = (b << 3) | (b >> 2);
            int g = ((rgb565 & 0x07E0) >> 5);
            g = (g << 2) | (g >> 4);
            int r = ((rgb565 & 0xF800) >> 11);
            r = (r << 3) | (r >> 2);
            return new RGBColor(r, g, b);
        }
    
        /// <summary>
        /// Returns bigger color, combined of max values from R, G, B channels
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static RGBColor Max(RGBColor value1, RGBColor value2)
        {
            RGBColor result = new RGBColor(
                Math.Max(value1.R, value2.R),
                Math.Max(value1.G, value2.G),
                Math.Max(value1.B, value2.B));
            return result;
        }

        /// <summary>
        /// Returns smaller color, combined of min values from R, G, B channels
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static RGBColor Min(RGBColor value1, RGBColor value2)
        {
            RGBColor result = new RGBColor(
                Math.Min(value1.R, value2.R),
                Math.Min(value1.G, value2.G),
                Math.Min(value1.B, value2.B));
            return result;
        }
       
    }
}
