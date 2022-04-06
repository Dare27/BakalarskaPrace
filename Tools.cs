using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
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

        public void Dithering(List<Point> points, Color color01, Color color02, WriteableBitmap bitmap)
        {
            foreach (Point point in points) 
            {
                int x = (int)point.X;
                int y = (int)point.Y;
                Color color;

                if ((x + y) % 2 == 0)
                {
                    color = color01;
                }
                else
                {
                    color = color02;
                }

                imageManipulation.AddPixel(x, y, color, bitmap);
            }
        }


        /*public void FloodFill(int x, int y, Color newColor, Color seedColor, WriteableBitmap bitmap, bool alphaBlending)
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
                    FloodFill(x - 1, y, newColor, seedColor, bitmap, alphaBlending);
                }
                if (x + 1 < bitmap.PixelWidth)
                {
                    FloodFill(x + 1, y, newColor, seedColor, bitmap, alphaBlending);
                }
                if (y - 1 > -1)
                {
                    FloodFill(x, y - 1, newColor, seedColor, bitmap, alphaBlending);
                }
                if (y + 1 < bitmap.PixelHeight)
                {
                    FloodFill(x, y + 1, newColor, seedColor, bitmap, alphaBlending);
                }
            }
        }*/

        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Rekurzivní verze může způsobit StackOverflowException při větších velikostech, proto musím používat nerekurzivní verzi tohoto alg.
        public List<Point> FloodFill(WriteableBitmap bitmap, Point point, Color seedColor, Color newColor, bool alphaBlending)
        {
            Stack<Point> pixels = new Stack<Point>();
            List<Point> visitedPoints = new List<Point>();
            pixels.Push(point);

            while (pixels.Count > 0)
            {
                Point currentPoint = pixels.Pop();
                if (currentPoint.X < bitmap.PixelWidth && currentPoint.X >= 0 && currentPoint.Y < bitmap.PixelHeight && currentPoint.Y >= 0 && !pixels.Contains(currentPoint))//make sure we stay within bounds
                {
                    Color currentColor = imageManipulation.GetPixelColor((int)currentPoint.X, (int)currentPoint.Y, bitmap);
                    if (currentColor == seedColor)
                    {
                        visitedPoints.Add(currentPoint);
                        if (!visitedPoints.Contains(new Point(currentPoint.X + 1, currentPoint.Y)))
                        {
                            pixels.Push(new Point(currentPoint.X + 1, currentPoint.Y));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X - 1, currentPoint.Y)))
                        {
                            pixels.Push(new Point(currentPoint.X - 1, currentPoint.Y));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X, currentPoint.Y + 1)))
                        {
                            pixels.Push(new Point(currentPoint.X, currentPoint.Y + 1));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X, currentPoint.Y - 1)))
                        {
                            pixels.Push(new Point(currentPoint.X, currentPoint.Y - 1));
                        }
                    }
                }
            }

            foreach (Point visitedPoint in visitedPoints) 
            {
                if (alphaBlending == true)
                {
                    Color colorMix = imageManipulation.ColorMix(newColor, imageManipulation.GetPixelColor((int)visitedPoint.X, (int)visitedPoint.Y, bitmap));
                    imageManipulation.AddPixel((int)visitedPoint.X, (int)visitedPoint.Y, colorMix, bitmap);
                }
                else
                {
                    imageManipulation.AddPixel((int)visitedPoint.X, (int)visitedPoint.Y, newColor, bitmap);
                }
            }

            return visitedPoints;
        }

        public void SpecialBucket(List<WriteableBitmap> bitmaps, Color newColor, Color seedColor, bool alphaBlending)
        {
            foreach (WriteableBitmap bitmap in bitmaps) 
            {
                for (int i = 0; i < bitmap.PixelWidth; i++)
                {
                    for (int j = 0; j < bitmap.PixelHeight; j++)
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
        }

        public void Shading(List<Point> points, WriteableBitmap bitmap, bool darken)
        {
            foreach (Point point in points)
            {
                double h, l, s;
                int r, g, b;
                int x = (int)point.X;
                int y = (int)point.Y;

                Color color;
                Color currentPixelColor = imageManipulation.GetPixelColor(x, y, bitmap);
                colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);

                if (darken == true) //else lighten
                {
                    l -= shadingStep;
                    if (l < 0)
                    {
                        l = 0;
                    }
                }
                else
                {
                    l += shadingStep;
                    if (l > 1)
                    {
                        l = 1;
                    }
                }

                colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
                color = Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b);
                imageManipulation.AddPixel(x, y, color, bitmap);
            }
        }

        public List<Point> SymmetricDrawing(int x, int y, WriteableBitmap bitmap)
        {
            int mirrorPostion = 0;
            List<Point> points = new List<Point>();

            //Chybí převrácení podle osy souměrnosti
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                //Použít horizontální a vertikální osu 
                if (x > bitmap.PixelWidth / 2)
                {
                    mirrorPostion = bitmap.PixelWidth - x - 1;
                    points.Add(new Point(mirrorPostion, y));
                }
                else
                {

                    int dif = (bitmap.PixelWidth / 2) - x;
                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                    points.Add(new Point(mirrorPostion, y));
                }

                if (y > bitmap.PixelHeight / 2)
                {
                    mirrorPostion = bitmap.PixelHeight - y - 1;
                    points.Add(new Point(x, mirrorPostion));
                }
                else
                {
                    int dif = (bitmap.PixelHeight / 2) - y;
                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                    points.Add(new Point(x, mirrorPostion));
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                //Použít horizontální osu 
                if (y > bitmap.PixelHeight / 2)
                {
                    mirrorPostion = bitmap.PixelHeight - y - 1;
                    points.Add(new Point(x, mirrorPostion));
                }
                else
                {
                    int dif = (bitmap.PixelHeight / 2) - y;
                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                    points.Add(new Point(x, mirrorPostion));
                }
            }
            else
            {
                //Použít vertikální osu 
                if (x > bitmap.PixelWidth / 2)
                {
                    mirrorPostion = bitmap.PixelWidth - x - 1;
                    points.Add(new Point(mirrorPostion, y));
                }
                else
                {
                    int dif = (bitmap.PixelWidth / 2) - x;
                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                    points.Add(new Point(mirrorPostion, y));
                }
            }
            points.Add(new Point(x, y));

            return points;
        }
    }
}
