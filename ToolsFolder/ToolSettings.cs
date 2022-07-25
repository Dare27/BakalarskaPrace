using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class ToolSettings
    {
        public Color AlphaBlending(bool alphaBleding, Color foregroundColor, Color backgroundColor)
        {
            if (alphaBleding == true)
            {
                byte a = (byte)(255 - ((255 - backgroundColor.A) * (255 - foregroundColor.A) / 255));
                byte r;
                byte g;
                byte b;

                if (backgroundColor.A != 0 && foregroundColor.A != 0)
                {
                    r = (byte)(((foregroundColor.A * foregroundColor.R) + ((255 - foregroundColor.A) * backgroundColor.R)) / 255);
                    g = (byte)(((foregroundColor.A * foregroundColor.G) + ((255 - foregroundColor.A) * backgroundColor.G)) / 255);
                    b = (byte)(((foregroundColor.A * foregroundColor.B) + ((255 - foregroundColor.A) * backgroundColor.B)) / 255);
                }
                else if (foregroundColor.A == 0) 
                {
                    a = (byte)backgroundColor.A;
                    r = (byte)backgroundColor.R;
                    g = (byte)backgroundColor.G;
                    b = (byte)backgroundColor.B;
                }
                else
                {
                    a = foregroundColor.A;
                    r = foregroundColor.R;
                    g = foregroundColor.G;
                    b = foregroundColor.B;
                }
                return Color.FromArgb(a, r, g, b);
            }
            else 
            {
                return foregroundColor;
            }
        }

        public List<System.Drawing.Point> StrokeThicknessSetter(WriteableBitmap bitmap, System.Drawing.Point point, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, bool previewBitmap = false)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            int size = thickness / 2;
            int isEven = 0;

            if (thickness % 2 != 0) isEven = 1;

            for (int i = -size; i < size + isEven; i++)
            {
                for (int j = -size; j < size + isEven; j++)
                {
                    //Zkontrolovat jestli se pixel vejde do bitmapy
                    if (point.X + i < width && point.X + i > -1 && point.Y + j < height && point.Y + j > -1)
                    {
                        System.Drawing.Point newPoint = new System.Drawing.Point(point.X + i, point.Y + j);

                        //Pokud se zapisuje do preview bitmapy tak kontrola navštívených bodů vede k smazání bodů 
                        if (previewBitmap == true)
                        {
                            points.Add(newPoint);
                        }
                        else
                        {
                            if (!undoPoints.Contains(newPoint))
                            {
                                undoPoints.Add(newPoint);
                                points.Add(newPoint);
                            }
                        }
                    }
                }
            }

            using (bitmap.GetBitmapContext())
            {
                foreach (System.Drawing.Point generatedPoint in points)
                {
                    Color currentColor = bitmap.GetPixel(generatedPoint.X, generatedPoint.Y);
                    if (previewBitmap == false)
                    {
                        undoColors.Add(currentColor);
                    }
                    Color finalColor = AlphaBlending(alphaBlending, color, currentColor);
                    bitmap.SetPixel(generatedPoint.X, generatedPoint.Y, finalColor);
                }
            }
            return points;
        }
    }
}
