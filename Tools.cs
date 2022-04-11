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

        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Rekurzivní verze může způsobit StackOverflowException při větších velikostech, proto musí být použíta nerekurzivní verzi tohoto alg.
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

            List<Point> points = new List<Point>();

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                //Vytvořit horizontální, vertikální a osu souměrnosti
                points.Add(CreateHorizontalPoint(x, y, bitmap.PixelHeight));
                points.Add(CreateVerticalPoint(x, y, bitmap.PixelWidth));
                points.Add(CreateAxialPoint(x, y, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                //Vytvořit horizontální osu 
                points.Add(CreateHorizontalPoint(x, y, bitmap.PixelHeight));
            }
            else
            {
                //Vytvořit vertikální osu 
                points.Add(CreateVerticalPoint(x, y, bitmap.PixelWidth));
            }
            points.Add(new Point(x, y));

            return points;
        }

        private Point CreateAxialPoint(int x, int y, int width, int height)
        {
            int mirrorPostionY = 0;
            int mirrorPostionX = 0;
            Point newPoint = new Point();

            if (y > height / 2 && x > width / 2)                            //Right down
            {
                mirrorPostionY = height - y - 1;
                mirrorPostionX = width - x - 1;
                newPoint = new Point(mirrorPostionX, mirrorPostionY);
            }
            else if (y <= height / 2 && x > width / 2)                      //Right up
            {
                int difX = (height / 2) - y;
                mirrorPostionY = (height / 2) + difX - 1;
                mirrorPostionX = width - x - 1;
                newPoint = new Point(mirrorPostionX, mirrorPostionY);
            }
            else if (y > height / 2 && x <= width / 2)                      //Left down
            {
                mirrorPostionY = height - y - 1;
                int difX = (width / 2) - x;
                mirrorPostionX = (width / 2) + difX - 1;
                newPoint = new Point(mirrorPostionX, mirrorPostionY);
            }
            else if (y <= height / 2 && x <= width / 2)                     //Left up
            {
                int difX = (height / 2) - y;
                mirrorPostionY = (height / 2) + difX - 1;
                int dif = (width / 2) - x;
                mirrorPostionX = (width / 2) + dif - 1;
                newPoint = new Point(mirrorPostionX, mirrorPostionY);
            }

            return newPoint;
        }

        private Point CreateHorizontalPoint(int x, int y, int height) 
        {
            Point newPoint;
            int mirrorPostion;

            if (y > height / 2)
            {
                mirrorPostion = height - y - 1;
                newPoint = new Point(x, mirrorPostion);
            }
            else
            {
                int dif = (height / 2) - y;
                mirrorPostion = (height / 2) + dif - 1;
                newPoint = new Point(x, mirrorPostion);
            }

            return newPoint;
        }

        private Point CreateVerticalPoint(int x, int y, int width)
        {
            Point newPoint;
            int mirrorPostion;

            if (x > width / 2)
            {
                mirrorPostion = width - x - 1;
                newPoint = new Point(mirrorPostion, y);
            }
            else
            {
                int dif = (width / 2) - x;
                mirrorPostion = (width / 2) + dif - 1;
                newPoint = new Point(mirrorPostion, y);
            }

            return newPoint;
        }
    }
}
