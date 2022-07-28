using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class FloodFillTool : ToolSettings, IBucket 
    {
        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Rekurzivní verze může způsobit StackOverflowException při větších velikostech, proto musí být použíta nerekurzivní verzi tohoto alg.
        public void GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point, Color color, bool alphaBledning, Dictionary<System.Drawing.Point, Color> undoPointColors/*, List<System.Drawing.Point> undoPoints, List<Color> undoColors*/)
        {
            Stack<System.Drawing.Point> points = new Stack<System.Drawing.Point>();
            Color seedColor = bitmap.GetPixel(point.X, point.Y);
            Color finalColor = AlphaBlending(alphaBledning, color, seedColor);
            points.Push(point);

            while (points.Count > 0)
            {
                System.Drawing.Point currentPoint = points.Pop();
                if (currentPoint.X < bitmap.PixelWidth && currentPoint.X >= 0 && currentPoint.Y < bitmap.PixelHeight && currentPoint.Y >= 0 && !points.Contains(currentPoint))//make sure we stay within bounds
                {
                    Color currentColor = bitmap.GetPixel(currentPoint.X, currentPoint.Y);
                    if (currentColor == seedColor && seedColor != finalColor)
                    {
                        undoPointColors.Add(currentPoint, currentColor);
                        bitmap.SetPixel(currentPoint.X, currentPoint.Y, finalColor);
                        points.Push(new System.Drawing.Point(currentPoint.X + 1, currentPoint.Y));
                        points.Push(new System.Drawing.Point(currentPoint.X - 1, currentPoint.Y));
                        points.Push(new System.Drawing.Point(currentPoint.X, currentPoint.Y + 1));
                        points.Push(new System.Drawing.Point(currentPoint.X, currentPoint.Y - 1));
                    }
                }
                Console.WriteLine(points.Count);
            }
        }
    }
}
