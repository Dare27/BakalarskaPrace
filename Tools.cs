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

        public List<Point> StrokeThicknessSetter(WriteableBitmap bitmap, WriteableBitmap previewBitmap, int x, int y, Color color, bool AlphaBlend, int thickness, List<Point> visitedPoints, List<Color> undoColors, List<Point> undoPoints)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            List<Point> points = new List<Point>();

            int size = thickness / 2;
            int isOdd = 0;

            if (thickness % 2 != 0) isOdd = 1;

            for (int i = -size; i < size + isOdd; i++)
            {
                for (int j = -size; j < size + isOdd; j++)
                {
                    //Zkontrolovat jestli se pixel vejde do bitmapy
                    if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                    {
                        Point point = new Point(x + i, y + j);

                        //Pokud se zapisuje do preview bitmapy tak kontrola navštívených bodů vede k smazání bodů 
                        if (bitmap == previewBitmap)
                        {
                            points.Add(point);
                        }
                        else
                        {
                            if (!visitedPoints.Contains(point))
                            {
                                visitedPoints.Add(point);
                                points.Add(point);
                            }
                        }
                    }
                }
            }

            using (bitmap.GetBitmapContext())
            {
                foreach (var point in points)
                {
                    if (bitmap != previewBitmap)
                    {
                        Color currentColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmap);
                        undoColors.Add(currentColor);
                        undoPoints.Add(point);
                    }

                    Color finalColor = color;
                    Color currentPixelColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmap);
                    if (AlphaBlend == true) finalColor = imageManipulation.ColorMix(color, currentPixelColor);
                    imageManipulation.AddPixel((int)point.X, (int)point.Y, finalColor, bitmap);
                }
            }
            return points;
        }

        public void Dithering(List<Point> points, Color color01, Color color02, WriteableBitmap bitmap, int strokeThickness, bool alphaBlending, List<Point> visitedPoints, List<Point> undoPoints, List<Color> undoColors)
        {
            if (visitedPoints == null) visitedPoints = new List<Point>();
            foreach (Point point in points) 
            {
                int size = strokeThickness / 2;
                int isOdd = 0;
                int x = (int)point.X;
                int y = (int)point.Y;
                Color color;
                Color colorMix;
                Color backgroundColor;

                if (strokeThickness % 2 != 0) isOdd = 1;

                for (int i = -size; i < size + isOdd; i++)
                {
                    for (int j = -size; j < size + isOdd; j++)
                    {
                        // zkontrolovat jestli se pixel vejde do bitmapy
                        if (x + i < bitmap.PixelWidth && x + i > -1 && y + j < bitmap.PixelHeight && y + j > -1)
                        {
                            Point newPoint = new Point(x + i, y + j);
                            if (!visitedPoints.Contains(newPoint))
                            {
                                visitedPoints.Add(newPoint);
                                if ((x + i + y + j) % 2 == 0)
                                {
                                    color = color01;
                                }
                                else
                                {
                                    color = color02;
                                }

                                backgroundColor = imageManipulation.GetPixelColor(x + i, y + j, bitmap);
                                if (alphaBlending == true)
                                {
                                    colorMix = imageManipulation.ColorMix(color, backgroundColor);
                                    imageManipulation.AddPixel(x + i, y + j, colorMix, bitmap);
                                }
                                else
                                {
                                    imageManipulation.AddPixel(x + i, y + j, color, bitmap);
                                }
                                undoPoints.Add(newPoint);
                                undoColors.Add(backgroundColor);
                            }
                        }
                    }
                }
            }
        }

        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Rekurzivní verze může způsobit StackOverflowException při větších velikostech, proto musí být použíta nerekurzivní verzi tohoto alg.
        public List<Point> FloodFill(WriteableBitmap bitmap, Point point, Color seedColor, Color newColor, bool alphaBlending)
        {
            Stack<Point> points = new Stack<Point>();
            List<Point> visitedPoints = new List<Point>();
            points.Push(point);

            while (points.Count > 0)
            {
                Point currentPoint = points.Pop();
                if (currentPoint.X < bitmap.PixelWidth && currentPoint.X >= 0 && currentPoint.Y < bitmap.PixelHeight && currentPoint.Y >= 0 && !points.Contains(currentPoint))//make sure we stay within bounds
                {
                    Color currentColor = imageManipulation.GetPixelColor((int)currentPoint.X, (int)currentPoint.Y, bitmap);
                    if (currentColor == seedColor)
                    {
                        visitedPoints.Add(currentPoint);
                        if (!visitedPoints.Contains(new Point(currentPoint.X + 1, currentPoint.Y)))
                        {
                            points.Push(new Point(currentPoint.X + 1, currentPoint.Y));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X - 1, currentPoint.Y)))
                        {
                            points.Push(new Point(currentPoint.X - 1, currentPoint.Y));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X, currentPoint.Y + 1)))
                        {
                            points.Push(new Point(currentPoint.X, currentPoint.Y + 1));
                        }

                        if (!visitedPoints.Contains(new Point(currentPoint.X, currentPoint.Y - 1)))
                        {
                            points.Push(new Point(currentPoint.X, currentPoint.Y - 1));
                        }
                    }
                }
            }

            /*foreach (Point visitedPoint in visitedPoints) 
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
            }*/

            return visitedPoints;
        }

        public List<Point> SpecialBucket(WriteableBitmap bitmap, Color newColor, Color seedColor, bool alphaBlending)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; i < bitmap.PixelWidth; i++)
            {
                for (int j = 0; j < bitmap.PixelHeight; j++)
                {
                    Color currentColor = imageManipulation.GetPixelColor(i, j, bitmap);
                    if (currentColor == seedColor)
                    {
                        points.Add(new Point(i, j));
                    }
                }
            }
            return points;
        }

        public void Shading(List<Point> points, WriteableBitmap bitmap, bool darken, int strokeThickness, List<Point> visitedPoints, List<Point> undoPoints, List<Color> undoColors)
        {
            if (visitedPoints == null) visitedPoints = new List<Point>();
            foreach (Point point in points)
            {
                int size = strokeThickness / 2;
                int isOdd = 0;
                int x = (int)point.X;
                int y = (int)point.Y;
                Color color;
                Color currentPixelColor;
                double h, l, s;
                int r, g, b;

                if (strokeThickness % 2 != 0) isOdd = 1;

                for (int i = -size; i < size + isOdd; i++)
                {
                    for (int j = -size; j < size + isOdd; j++)
                    {
                        // zkontrolovat jestli se pixel vejde do bitmapy
                        if (x + i < bitmap.PixelWidth && x + i > -1 && y + j < bitmap.PixelHeight && y + j > -1)
                        {
                            Point newPoint = new Point(x + i, y + j);
                            if (!visitedPoints.Contains(newPoint))
                            {
                                visitedPoints.Add(newPoint);
                                currentPixelColor = imageManipulation.GetPixelColor(x + i, y + j, bitmap);
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
                                imageManipulation.AddPixel(x + i, y + j, color, bitmap);
                                undoPoints.Add(newPoint);
                                undoColors.Add(currentPixelColor);
                            }
                        }
                    }
                }
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
