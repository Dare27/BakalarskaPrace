﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class ImageManipulation
    {
        public void AddPixel(int x, int y, Color color, WriteableBitmap bitmap)
        {
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
            }
        }

        public unsafe Color GetPixelColor(int x, int y, WriteableBitmap bitmap)
        {
            Color pix = new Color();
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
            return pix;
        }

        public Color ColorMix(Color foregroundColor, Color backgroundColor)
        {
            byte a = (byte)(255 - ((255 - backgroundColor.A) * (255 - foregroundColor.A) / 255));
            byte r = (byte)((backgroundColor.R * (255 - foregroundColor.A) + foregroundColor.R * foregroundColor.A) / 255);
            byte g = (byte)((backgroundColor.G * (255 - foregroundColor.A) + foregroundColor.G * foregroundColor.A) / 255);
            byte b = (byte)((backgroundColor.B * (255 - foregroundColor.A) + foregroundColor.B * foregroundColor.A) / 255);
            return Color.FromArgb(a, r, g, b);
        }

        public void Eraser(int x, int y, WriteableBitmap writeableBitmap, int strokeThickness, int width, int height)
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
                    if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                    {
                        byte[] ColorData = { 0, 0, 0, 0 }; // B G R
                        Int32Rect rect = new Int32Rect(x + i, y + j, 1, 1);
                        writeableBitmap.WritePixels(rect, ColorData, 4, 0);
                    }
                }
            }
        }

        public void Clear(WriteableBitmap bitmap, int width, int height)
        {

            //Array.Clear(writeableBitmap, 0, (int)writeableBitmap.PixelWidth * (int)writeableBitmap.Height);
            

            /*for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    byte[] ColorData = { 0, 0, 0, 0 }; // B G R
                    Int32Rect rect = new Int32Rect(i, j, 1, 1);
                    writeableBitmap.WritePixels(rect, ColorData, 16, 0);
                }
            }*/
        }
    }
}