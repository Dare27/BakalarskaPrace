using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class DitheringTool: ToolSettings
    {
        public void GeneratePoints(List<System.Drawing.Point> points, Color color01, Color color02, WriteableBitmap bitmap, int strokeThickness, bool alphaBlending, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            foreach (System.Drawing.Point point in points)
            {
                int size = strokeThickness / 2;
                int isOdd = 0;
                int x = point.X;
                int y = point.Y;
                Color color;
                Color backgroundColor;

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
                                if ((x + i + y + j) % 2 == 0)
                                {
                                    color = color01;
                                }
                                else
                                {
                                    color = color02;
                                }

                                backgroundColor = bitmap.GetPixel(x + i, y + j);
                                color = AlphaBlending(alphaBlending, color, backgroundColor);
                                bitmap.SetPixel(x + i, y + j, color);
                                undoColors.Add(backgroundColor);
                            }
                        }
                    }
                }
            }
        }
    }
}
