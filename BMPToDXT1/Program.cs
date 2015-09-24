using System;

namespace BMPDDSConverter
{
    class Program
    {
        static void Main(string[] args)
        {
           
            while(true)
            {
                Console.WriteLine("Load file - write name");
                string fileName = Console.ReadLine();
                ProccesImage(fileName);
            }
          
        }


        private static void ProccesImage(string fileName)
        {
            if (fileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)) 
            {
                BMPImage bmp = new BMPImage(fileName.Remove(fileName.Length-4));
                DDSImage dds = ImageCompression.ConvertBMPToDDS(bmp);
                Console.WriteLine("Saved");
            }
            else if(fileName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                DDSImage dds = new DDSImage(fileName.Remove(fileName.Length-4));
                BMPImage bmp = ImageCompression.ConvertDDSToBMP(dds);
                Console.WriteLine("Saved");
            }
            else
            {
                Console.WriteLine(fileName+ " File not supported");
            }

        }

    }
}
