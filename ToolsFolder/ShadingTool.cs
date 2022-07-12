using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class ShadingTool
    {
        ColorSpaceConvertor colorSpaceConvertor = new ColorSpaceConvertor();

        public void GeneratePoints(List<System.Drawing.Point> points, WriteableBitmap bitmap, bool darken, int strokeThickness, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            foreach (System.Drawing.Point point in points)
            {
                int size = strokeThickness / 2;
                int isOdd = 0;
                int x = point.X;
                int y = point.Y;
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
                            System.Drawing.Point newPoint = new System.Drawing.Point(x + i, y + j);
                            if (!undoPoints.Contains(newPoint))
                            {
                                undoPoints.Add(newPoint);
                                currentPixelColor = bitmap.GetPixel(x + i, y + j);
                                colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);

                                if (darken == true) //else lighten
                                {
                                    l -= 0.1;
                                    if (l < 0)
                                    {
                                        l = 0;
                                    }
                                }
                                else
                                {
                                    l += 0.1;
                                    if (l > 1)
                                    {
                                        l = 1;
                                    }
                                }

                                colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
                                color = Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b);
                                bitmap.SetPixel(x + i, y + j, color);
                                undoColors.Add(currentPixelColor);
                            }
                        }
                    }
                }
            }
        }
    }
}
