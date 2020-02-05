using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Brightness
{
    class ImageOperator
    {
        public Image image { get; set; }

        public Bitmap bitmap { get; set; }

        public Image afterImage { get; set; }

        public byte[] red { get; set; }

        public byte[] green { get; set; }

        public byte[] blue { get; set; }

        public double time { get; set; }

        const int size = 16;

        private BitmapData bmpData;

        [DllImport("C_PROC.dll")]
        static extern void LightenC(IntPtr tab, byte ratio);

        [DllImport("C_PROC.dll")]
        static extern void DimC(IntPtr tab, byte ratio);

        [DllImport("ASM_PROC.dll")]
        static extern void LightenASM(IntPtr tab, byte ratio);
        [DllImport("ASM_PROC.dll")]
        static extern void DimASM(IntPtr tab, byte ratio);
        public void bitmapFromImage()
        {
            bitmap = (Bitmap)image;
        }

        public void imageFromBitmap()
        {
            image = (Image)bitmap;
        }

        // funkcja tworząca tablice r g b z obrazu
        public void createRGB()
        {

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            int bytes = bmpData.Stride * bitmap.Height;

            byte[] rgbValues = new byte[bytes];
            red = new byte[bytes / 3];
            green = new byte[bytes / 3];
            blue = new byte[bytes / 3];

            Marshal.Copy(ptr, rgbValues, 0, bytes);
            int count = 0;
            int stride = bmpData.Stride;

            // petla tworzące tablice r g b na podstawie bitmap 
            for (int column = 0; column < bmpData.Height; column++)
            {
                for (int row = 0; row < bmpData.Width; row++)
                {
                    blue[count] = (byte)(rgbValues[(column * stride) + (row * 3)]);
                    green[count] = (byte)(rgbValues[(column * stride) + (row * 3) + 1]);
                    red[count++] = (byte)(rgbValues[(column * stride) + (row * 3) + 2]);
                }
            }
            bitmap.UnlockBits(bmpData);
        }

        public void Brighten(int ratio, bool isAsm, int cores)
        {
            // dane dzielone są na 16 bajtowe wektory, rozmiar tablic - ilosc pikseli / 16 (size = 16)
            int arraySize = red.Length / size;
            IntPtr[] redArray = new IntPtr[arraySize];
            IntPtr[] greenArray = new IntPtr[arraySize];
            IntPtr[] blueArray = new IntPtr[arraySize];

            // funkcja alokujące pamięć tablic

            AllocateArrays(redArray, greenArray, blueArray, arraySize);
            if (ratio >= 0)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (isAsm)
                {
                    Parallel.For(0, arraySize, new ParallelOptions { MaxDegreeOfParallelism = cores }, i =>
                    {
                        LightenASM(redArray[i], (byte)ratio);
                        LightenASM(greenArray[i], (byte)ratio);
                        LightenASM(blueArray[i], (byte)ratio);
                    });
                }
                else
                {
                    Parallel.For(0, arraySize, new ParallelOptions { MaxDegreeOfParallelism = cores }, i =>
                    {
                        LightenC(redArray[i], (byte)ratio);
                        LightenC(greenArray[i], (byte)ratio);
                        LightenC(blueArray[i], (byte)ratio);
                    });
                }
                stopwatch.Stop();
                time = stopwatch.Elapsed.TotalMilliseconds;
            }
            else if (ratio < 0)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (isAsm)
                {
                    Parallel.For(0, arraySize, new ParallelOptions { MaxDegreeOfParallelism = cores }, i =>
                    {
                        DimASM(redArray[i], (byte)Math.Abs(ratio));
                        DimASM(greenArray[i], (byte)Math.Abs(ratio));
                        DimASM(blueArray[i], (byte)Math.Abs(ratio));
                    });
                }
                else
                {
                    Parallel.For(0, arraySize, new ParallelOptions { MaxDegreeOfParallelism = cores }, i =>
                    {
                        DimC(redArray[i], (byte)Math.Abs(ratio));
                        DimC(greenArray[i], (byte)Math.Abs(ratio));
                        DimC(blueArray[i], (byte)Math.Abs(ratio));
                    });
                }
                stopwatch.Stop();
                time = stopwatch.Elapsed.TotalMilliseconds;
            }
            
         
       
            // Przypisanie nowych wartosci
            AssignNewValues(redArray, greenArray, blueArray, arraySize, ratio);  
            
            //zwolnienie pamieci
            for(int i = 0; i<arraySize; i++)
            {
                Marshal.FreeHGlobal(redArray[i]);
                Marshal.FreeHGlobal(greenArray[i]);
                Marshal.FreeHGlobal(blueArray[i]);
            }
        }

        private void AllocateArrays(IntPtr [] redArray, IntPtr[] greenArray, IntPtr[] blueArray, int arraySize)
        {
            int begin = 0;

            for (int i = 0; i < arraySize; i++)
            {
                // każda z tablic musi przechowywać 16 pikseli = bajt * 16
                redArray[i] = Marshal.AllocHGlobal(sizeof(byte) * size);
                greenArray[i] = Marshal.AllocHGlobal(sizeof(byte) * size);
                blueArray[i] = Marshal.AllocHGlobal(sizeof(byte) * size);

                // kopiowanie wartości r g b do nowych tablic 
                Marshal.Copy(red, begin, redArray[i], size);
                Marshal.Copy(green, begin, greenArray[i], size);
                Marshal.Copy(blue, begin, blueArray[i], size);

                begin += size;
            }
        }
        
        // funkcja poprawiająca wartości po sumowaniu (sprawdza czy przekroczyły zakres)
        private void AssignNewValues(IntPtr[] redArray, IntPtr[] greenArray, IntPtr[] blueArray, int arraySize, int ratio)
        {
            byte[] r = new byte[arraySize * size];
            byte[] g = new byte[arraySize * size];
            byte[] b = new byte[arraySize * size];
            int counter = 0;
            bool pos = ratio > 0 ? true : false;
            byte rb;
            byte gb;
            byte bb;
            for (int i = 0; i < arraySize; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //odczytanie nowych wartosci r g b piksela
                    rb = Marshal.ReadByte(redArray[i] + j);
                    gb = Marshal.ReadByte(greenArray[i] + j);
                    bb = Marshal.ReadByte(blueArray[i] + j);
                    // przypisanie do r g b
                    r[counter] = rb;
                    g[counter] = gb;
                    b[counter] = bb;
                    counter++;
                }
            }
            // przypisanie nowych wartości
            red = r;
            green = g;
            blue = b;
        }

        // funkcja tworząca obraz na podstawie tablic r g b
        public void AfterImageFromRGB()
        {

            Bitmap pic = new Bitmap(bmpData.Width, bmpData.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Color c;
            int arrayIndex=0;
            int width = bmpData.Width;
            int height = bmpData.Height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    arrayIndex = y * bmpData.Width + x; // obliczenie indeksu
                    if (arrayIndex>red.Length-1)
                    {
                        break;
                    }
                    c = Color.FromArgb(red[arrayIndex], green[arrayIndex], blue[arrayIndex]); // znajdywanie koloru na podstawie wartości rgb
                    pic.SetPixel(x, y, c); // ustawianie piksela
                }
            }
            afterImage = (Image)pic;
        }
    }
}
