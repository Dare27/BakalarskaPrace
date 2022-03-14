using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class Tools
    {
        ImageManipulation imageManipulation;
        ColorSpaceConvertor colorSpaceConvertor;
        double shadingStep = .1;

        public Tools()
        {
            imageManipulation = new ImageManipulation();
            colorSpaceConvertor = new ColorSpaceConvertor();
        }

        public void Dithering(int x, int y, Color color01, Color color02, WriteableBitmap bitmap)
        {
            if ((x + y) % 2 == 0)
            {
                imageManipulation.AddPixel(x, y, color01, bitmap);
            }
            else
            {
                imageManipulation.AddPixel(x, y, color02, bitmap);
            }
        }

        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Může způsobit StackOverflowException při větších velikostech
        public void FloodFill(int x, int y, Color newColor, Color seedColor, WriteableBitmap bitmap, bool alphaBlending, int width, int height)
        {
            Color currentColor = imageManipulation.GetPixelColor(x, y, bitmap);
            if (currentColor != newColor && currentColor == seedColor)
            {
                if (alphaBlending == true)
                {
                    Color colorMix = imageManipulation.ColorMix(newColor, currentColor);
                    imageManipulation.AddPixel(x, y, colorMix, bitmap);
                }
                else
                {
                    imageManipulation.AddPixel(x, y, newColor, bitmap);
                }

                if (x - 1 > -1)
                {
                    FloodFill(x - 1, y, newColor, seedColor, bitmap, alphaBlending, width, height);
                }
                if (x + 1 < width)
                {
                    FloodFill(x + 1, y, newColor, seedColor, bitmap, alphaBlending, width, height);
                }
                if (y - 1 > -1)
                {
                    FloodFill(x, y - 1, newColor, seedColor, bitmap, alphaBlending, width, height);
                }
                if (y + 1 < height)
                {
                    FloodFill(x, y + 1, newColor, seedColor, bitmap, alphaBlending, width, height);
                }
            }
        }

        public void SpecialBucket(int x, int y, WriteableBitmap bitmap, Color newColor, Color seedColor, bool alphaBlending, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color currentColor = imageManipulation.GetPixelColor(i, j, bitmap);
                    if (currentColor == seedColor)
                    {
                        if (alphaBlending == true)
                        {
                            Color colorMix = imageManipulation.ColorMix(newColor, currentColor);
                            imageManipulation.AddPixel(i, j, colorMix, bitmap);
                        }
                        else
                        {
                            imageManipulation.AddPixel(i, j, newColor, bitmap);
                        }
                    }
                }
            }
        }

        public void Lighten(int x, int y, WriteableBitmap bitmap)
        {
            double h;
            double l;
            double s;
            int r;
            int g;
            int b;

            Color currentPixelColor = imageManipulation.GetPixelColor(x, y, bitmap);
            colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);
            l += shadingStep;
            if (l > 1)
            {
                l = 1;
            }
            colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
            imageManipulation.AddPixel(x, y, Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b), bitmap);
        }

        public void Darken(int x, int y, WriteableBitmap bitmap)
        {
            double h;
            double l;
            double s;
            int r;
            int g;
            int b;

            Color currentPixelColor = imageManipulation.GetPixelColor(x, y, bitmap);
            colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);
            l -= shadingStep;
            if (l < 0)
            {
                l = 0;
            }
            colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
            imageManipulation.AddPixel(x, y, Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b), bitmap);
        }

        public List<Vector2> SymmetricDrawing(int x, int y, Color color, WriteableBitmap bitmap)
        {
            int mirrorPostion = 0;
            List<Vector2> points = new List<Vector2>();

            //Chybí převrácení podle osy souměrnosti
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                //Použít horizontální a vertikální osu 
                if (x > bitmap.PixelWidth / 2)
                {
                    mirrorPostion = bitmap.PixelWidth - x - 1;
                    points.Add(new Vector2(mirrorPostion, y));
                }
                else
                {

                    int dif = (bitmap.PixelWidth / 2) - x;
                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                    points.Add(new Vector2(mirrorPostion, y));
                }

                if (y > bitmap.PixelHeight / 2)
                {
                    mirrorPostion = bitmap.PixelHeight - y - 1;
                    points.Add(new Vector2(x, mirrorPostion));
                }
                else
                {
                    int dif = (bitmap.PixelHeight / 2) - y;
                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                    points.Add(new Vector2(x, mirrorPostion));
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                //Použít horizontální osu 
                if (y > bitmap.PixelHeight / 2)
                {
                    mirrorPostion = bitmap.PixelHeight - y - 1;
                    points.Add(new Vector2(x, mirrorPostion));
                }
                else
                {
                    int dif = (bitmap.PixelHeight / 2) - y;
                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                    points.Add(new Vector2(x, mirrorPostion));
                }
            }
            else
            {
                //Použít vertikální osu 
                if (x > bitmap.PixelWidth / 2)
                {
                    mirrorPostion = bitmap.PixelWidth - x - 1;
                    points.Add(new Vector2(mirrorPostion, y));
                }
                else
                {
                    int dif = (bitmap.PixelWidth / 2) - x;
                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                    points.Add(new Vector2(mirrorPostion, y));
                }
            }
            points.Add(new Vector2(x, y));

            return points;
        }
    }
}
