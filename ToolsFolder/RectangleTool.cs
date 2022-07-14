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
            int x0 = startPoint.X;
            int y0 = startPoint.Y;
            int x1 = endPoint.X;
            int y1 = endPoint.Y;

            if (alternativeFunction01)
            {
                int xDistance = (int)Math.Abs(x0 - x1);
                int yDistance = (int)Math.Abs(y0 - y1);
                int dif = Math.Abs(yDistance - xDistance);

                //Delší stranu je nutné zkrátit o rozdíl, poté se dá použít stejná funkce pro kreslení obdélníků 
                if (xDistance < yDistance)
                {
                    if (y0 < y1)
                    {
                        y1 = y1 - dif;
                    }
                    else
                    {
                        //Prohození souřadnic a přičtení rozdílu velikosti stran
                        int y = y1;
                        y1 = y0;
                        y0 = y + dif;
                    }
                }
                else
                {
                    if (x0 < x1)
                    {
                        x1 = x1 - dif;
                    }
                    else
                    {
                        //Prohození souřadnic a přičtení rozdílu velikosti stran
                        int x = x1;
                        x1 = x0;
                        x0 = x + dif;
                    }
                }
            }

            if (y0 < y1)
            {
                for (int y = y0; y <= y1; y++)
                {
                    if (alternativeFunction02)
                    {
                        List<System.Drawing.Point> fillPoints = DrawHorizontalLine(x1, x0, y);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new System.Drawing.Point(x0, y));
                        points.Add(new System.Drawing.Point(x1, y));
                    }
                }
            }
            else
            {
                for (int y = y0; y >= y1; y--)
                {
                    if (alternativeFunction02)
                    {
                        List<System.Drawing.Point> fillPoints = DrawHorizontalLine(x0, x1, y);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new System.Drawing.Point(x0, y));
                        points.Add(new System.Drawing.Point(x1, y));
                    }
                }
            }

            if (x0 < x1)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (alternativeFunction02)
                    {
                        List<System.Drawing.Point> fillPoints = DrawVerticalLine(y0, y1, x);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new System.Drawing.Point(x, y0));
                        points.Add(new System.Drawing.Point(x, y1));
                    }
                }
            }
            else
            {
                for (int x = x0; x >= x1; x--)
                {
                    if (alternativeFunction02)
                    {
                        List<System.Drawing.Point> fillPoints = DrawVerticalLine(y1, y0, x);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new System.Drawing.Point(x, y0));
                        points.Add(new System.Drawing.Point(x, y1));

                    }
                }
            }
            points.Add(new System.Drawing.Point(x1, y1));
            return points;
        }

        public List<System.Drawing.Point> DrawHorizontalLine(int x0, int x1, int y)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            for (int x = x0; x <= x1; x++)
            {
                points.Add(new System.Drawing.Point(x, y));
            }
            return points;
        }

        public List<System.Drawing.Point> DrawVerticalLine(int y0, int y1, int x)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            for (int y = y0; y <= y1; y++)
            {
                points.Add(new System.Drawing.Point(x, y));
            }
            return points;
        }
    }
}
