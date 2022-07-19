using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class RectangleTool : IGeometricTool
    {
        public List<System.Drawing.Point> GeneratePoints (WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            int sx = startPoint.X < endPoint.X ? 1 : -1;
            int sy = startPoint.Y < endPoint.Y ? 1 : -1;

            if (alternativeFunction01)
            {
                int dx = Math.Abs(startPoint.X - endPoint.X);
                int dy = Math.Abs(startPoint.Y - endPoint.Y);
                int dif = Math.Abs(dy - dx);

                //Delší stranu je nutné zkrátit o rozdíl, poté se dá použít stejná funkce pro kreslení obdélníků 
                if (dx < dy)
                {
                    if (startPoint.Y < endPoint.Y) endPoint.Y = endPoint.Y - dif;
                    else endPoint.Y = endPoint.Y + dif;
                }
                else
                {
                    if (startPoint.X < endPoint.X) endPoint.X = endPoint.X - dif;
                    else endPoint.X = endPoint.X + dif;
                }
            }

            int y = startPoint.Y;
            int x = startPoint.X;

            while (true)
            {
                if (alternativeFunction02)
                {
                    List<System.Drawing.Point> fillPoints = DrawHorizontalLine(endPoint.X, startPoint.X, y);
                    points.AddRange(fillPoints);
                }
                else
                {
                    points.Add(new System.Drawing.Point(startPoint.X, y));
                    points.Add(new System.Drawing.Point(endPoint.X, y));
                }
                if (y == endPoint.Y) break; 
                y += sy;
            }

            //Pro vyplněný tvar není nutné tyto body znovu vykreslovat
            if (alternativeFunction02 == false)
            {
                while (true)
                {
                    points.Add(new System.Drawing.Point(x, startPoint.Y));
                    points.Add(new System.Drawing.Point(x, endPoint.Y));
                    if (x == endPoint.X) break;
                    x += sx;
                }
            }

            points.Add(new System.Drawing.Point(startPoint.X, startPoint.Y));
            return points;
        }

        public List<System.Drawing.Point> DrawHorizontalLine(int x0, int x1, int y)
        {
            int tempX;
            if (x0 > x1) 
            {   
                tempX = x0;
                x0 = x1;
                x1 = tempX;
            } 

            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            for (int x = x0; x <= x1; x++)
            {
                points.Add(new System.Drawing.Point(x, y));
            }
            return points;
        }
    }
}
