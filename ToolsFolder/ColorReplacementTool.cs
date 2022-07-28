using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class ColorReplacementTool : ToolSettings, IBucket
    {
        public void GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point, Color color, bool alphaBlending, Dictionary<System.Drawing.Point, Color> undoPointColors/*, List<System.Drawing.Point> undoPoints, List<Color> undoColors*/)
        {
            Color seedColor = bitmap.GetPixel(point.X, point.Y);
            Color finalColor = AlphaBlending(alphaBlending, color, seedColor);
            for (int i = 0; i < bitmap.PixelWidth; i++)
            {
                for (int j = 0; j < bitmap.PixelHeight; j++)
                {
                    Color currentColor = bitmap.GetPixel(i, j);
                    if (currentColor == seedColor)
                    {
                        System.Drawing.Point currentPoint = new System.Drawing.Point(i, j);
                        undoPointColors.Add(currentPoint, currentColor);
                        bitmap.SetPixel(i, j, finalColor);
                    }
                }
            }
        }
    }
}
