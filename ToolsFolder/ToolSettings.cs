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
        public virtual Color AlphaBlending(bool alphaBleding, Color foregroundColor, Color backgroundColor)
        {
            if (alphaBleding == true)
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
            else 
            {
                return foregroundColor;
            }
        }

        public virtual List<System.Drawing.Point> StrokeThicknessSetter(WriteableBitmap bitmap, WriteableBitmap previewBitmap, int x, int y, Color color, bool alphaBlending, int thickness, List<System.Drawing.Point> visitedPoints, List<Color> undoColors, List<System.Drawing.Point> undoPoints)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

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
                        System.Drawing.Point point = new System.Drawing.Point(x + i, y + j);

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
                foreach (System.Drawing.Point point in points)
                {
                    Color currentColor = bitmap.GetPixel(point.X, point.Y);
                    if (bitmap != previewBitmap)
                    {
                        
                        undoColors.Add(currentColor);
                        undoPoints.Add(point);
                    }
                    Color finalColor = AlphaBlending(alphaBlending, color, currentColor);
                    bitmap.SetPixel(point.X, point.Y, finalColor);
                }
            }
            return points;
        }
    }
}
