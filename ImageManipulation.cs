using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class ImageManipulation
    {
        public void AddPixel(int x, int y, Color color, WriteableBitmap bitmap)
        {
            bitmap.SetPixel(x, y, color);
            /*
            try
            {
                // Reserve the back buffer for updates.
                bitmap.Lock();
                unsafe
                {
                    IntPtr pBackBuffer = bitmap.BackBuffer;

                    pBackBuffer += y * bitmap.BackBufferStride;
                    pBackBuffer += x * 4;

                    int colorData = color.A << 24;       // A
                    colorData |= color.R << 16;          // R
                    colorData |= color.G << 8;           // G
                    colorData |= color.B << 0;           // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = colorData;

                    bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                }
            }
            finally
            {
                // Release the back buffer and make it available for display.
                bitmap.Unlock();
            }*/
        }

        public unsafe Color GetPixelColor(int x, int y, WriteableBitmap bitmap)
        {
            /*Color pix = new Color();
            byte[] colorData = { 0, 0, 0, 0 }; // ARGB
            IntPtr pBackBuffer = bitmap.BackBuffer;
            byte* pBuff = (byte*)pBackBuffer.ToPointer();
            var a = pBuff[4 * x + (y * bitmap.BackBufferStride) + 3];
            var r = pBuff[4 * x + (y * bitmap.BackBufferStride) + 2];
            var g = pBuff[4 * x + (y * bitmap.BackBufferStride) + 1];
            var b = pBuff[4 * x + (y * bitmap.BackBufferStride)];
            pix.A = a;
            pix.R = r;
            pix.G = g;
            pix.B = b;
            return pix;*/
            Color color = bitmap.GetPixel(x, y);
            return color;

        }

        public Color ColorMix(Color foregroundColor, Color backgroundColor)
        {
            byte a = (byte)(255 - ((255 - backgroundColor.A) * (255 - foregroundColor.A) / 255));
            byte r;
            byte g;
            byte b;

            if (backgroundColor.A != 0)
            {
                r = (byte)((backgroundColor.R * (255 - foregroundColor.A) + foregroundColor.R * foregroundColor.A) / 255);
                g = (byte)((backgroundColor.G * (255 - foregroundColor.A) + foregroundColor.G * foregroundColor.A) / 255);
                b = (byte)((backgroundColor.B * (255 - foregroundColor.A) + foregroundColor.B * foregroundColor.A) / 255);
            }
            else 
            {
                r = foregroundColor.R;
                g = foregroundColor.G;
                b = foregroundColor.B;
            }
            return Color.FromArgb(a, r, g, b);
        }

        public void Eraser(int x, int y, WriteableBitmap bitmap, int strokeThickness)
        {
            int size = strokeThickness / 2;
            int isOdd = 0;

            if (strokeThickness % 2 != 0)
            {
                isOdd = 1;
            }

            for (int i = -size; i < size + isOdd; i++)
            {
                for (int j = -size; j < size + isOdd; j++)
                {
                    // zkontrolovat jestli se pixel vejde do bitmapy
                    if (x + i < bitmap.PixelWidth && x + i > -1 && y + j < bitmap.PixelHeight && y + j > -1)
                    {
                        byte[] ColorData = { 0, 0, 0, 0 }; // B G R
                        Int32Rect rect = new Int32Rect(x + i, y + j, 1, 1);
                        bitmap.WritePixels(rect, ColorData, 4, 0);
                    }
                }
            }
        }
    }
}
