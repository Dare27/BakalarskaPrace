using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class ColorReplacementTool : IBucket
    {
        public void GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point, Color color, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            Color seedColor = bitmap.GetPixel(point.X, point.Y);
            for (int i = 0; i < bitmap.PixelWidth; i++)
            {
                for (int j = 0; j < bitmap.PixelHeight; j++)
                {
                    Color currentColor = bitmap.GetPixel(i, j);
                    if (currentColor == seedColor)
                    {
                        System.Drawing.Point currentPoint = new System.Drawing.Point(i, j);
                        undoPoints.Add(currentPoint);
                        undoColors.Add(currentColor);
                        bitmap.SetPixel(i, j, color);
                    }
                }
            }
        }
    }
}
