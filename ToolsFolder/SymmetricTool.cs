using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class SymmetricTool : IBrush
    {
        public List<Point> GeneratePoints(WriteableBitmap bitmap, Point point)
        { 
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                //Vytvořit horizontální, vertikální a osu souměrnosti
                points.Add(CreateHorizontalPoint(point.X, point.Y, bitmap.PixelHeight));
                points.Add(CreateVerticalPoint(point.X, point.Y, bitmap.PixelWidth));
                points.Add(CreateAxialPoint(point.X, point.Y, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                //Vytvořit vertikální osu 
                points.Add(CreateVerticalPoint(point.X, point.Y, bitmap.PixelWidth));
            }
            else
            {
                //Vytvořit horizontální osu 
                points.Add(CreateHorizontalPoint(point.X, point.Y, bitmap.PixelHeight));
            }
            points.Add(point);

            return points;
        }

        private System.Drawing.Point CreateAxialPoint(int x, int y, int width, int height)
        {
            int mirrorPostionY = height - y - 1;
            int mirrorPostionX = width - x - 1;
            System.Drawing.Point newPoint = new System.Drawing.Point(mirrorPostionX, mirrorPostionY);
            return newPoint;
        }

        private System.Drawing.Point CreateVerticalPoint(int x, int y, int height)
        {
            int mirrorPostion = height - y - 1;
            System.Drawing.Point newPoint = new System.Drawing.Point(x, mirrorPostion);
            return newPoint;
        }

        private System.Drawing.Point CreateHorizontalPoint(int x, int y, int width)
        {
            int mirrorPostion = width - x - 1;
            System.Drawing.Point newPoint = new System.Drawing.Point(mirrorPostion, y);
            return newPoint;
        }
    }
}
