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
        public List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            Color seedColor = bitmap.GetPixel(point.X, point.Y);
            for (int i = 0; i < bitmap.PixelWidth; i++)
            {
                for (int j = 0; j < bitmap.PixelHeight; j++)
                {
                    Color currentColor = bitmap.GetPixel(i, j);
                    if (currentColor == seedColor)
                    {
                        points.Add(new System.Drawing.Point(i, j));
                    }
                }
            }
            return points;
        }
    }
}
