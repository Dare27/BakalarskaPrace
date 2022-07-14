using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class EllipseTool:IGeometricTool
    {
        //V případě této aplikace je nutné používat Mid-Point algoritmus, protože Bresenhaimův algoritmus nedosahuje při nízkých velikostech kruhu vzhledného výsledku
        public List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02)
        {
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
                        int tempY = y1;
                        y1 = y0;
                        y0 = tempY + dif;
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
                        int tempX = x1;
                        x1 = x0;
                        x0 = tempX + dif;
                    }
                }
            }

            //Částečné posunutí 
            int horizontalOdd;
            if ((x0 + x1) % 2 != 0) horizontalOdd = 1;
            else horizontalOdd = 0;

            int verticalOdd;
            if ((y0 + y1) % 2 != 0) verticalOdd = 1;
            else verticalOdd = 0;

            double centerX = (x0 + x1) / 2;
            double centerY = (y0 + y1) / 2;
            double radX = centerX - Math.Min(x0, x1);
            double radY = centerY - Math.Min(y0, y1);
            double radX2 = radX * radX;
            double radY2 = radY * radY;
            double twoRadX2 = 2 * radX2;
            double twoRadY2 = 2 * radY2;
            double p;
            double x = 0;
            double y = radY;
            double px = 0;
            double py = twoRadX2 * y;
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            //Vykreslení počátečního bodu do každého kvadrantu
            points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, alternativeFunction02, verticalOdd, horizontalOdd));

            //Počáteční rozhodovací parametr regionu 1
            p = (int)Math.Round(radY2 - (radX2 * radY) + (0.25 * radX2));

            //Vykreslení 1. regionu - horní/dolní část
            while (px < py)
            {
                x++;
                px += twoRadY2;
                //Kontrola a aktualizace hodnoty rozhodovacího parametru na základě algoritmu
                if (p < 0)
                {
                    p += radY2 + px;
                }
                else
                {
                    y--;
                    py -= twoRadX2;
                    p += radY2 + px - py;
                }
                points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, alternativeFunction02, verticalOdd, horizontalOdd));
            }

            //Počáteční rozhodovací parametr regionu 
            p = (int)Math.Round(radY2 * (x + 0.5) * (x + 0.5) + radX2 * (y - 1) * (y - 1) - radX2 * radY2);

            //Vykreslení 2. regionu - pravá/levá část
            while (y > 0)
            {
                y--;
                py -= twoRadX2;
                //Kontrola a aktualizace hodnoty rozhodovacího parametru na základě algoritmu
                if (p > 0)
                {
                    p += radX2 - py;
                }
                else
                {
                    x++;
                    px += twoRadX2;
                    p += radX2 - py + px;
                }
                points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, alternativeFunction02, verticalOdd, horizontalOdd));
            }
            return points;
        }

        //Vykreslit symetrické body ve všech kvadrantech pomocí souřadnic
        public List<System.Drawing.Point> QuadrantPlotter(int centerX, int centerY, int x, int y, bool fill, int verticalOdd, int horizontalOdd)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>(); ;
            if (fill)
            {
                points.AddRange(DrawHorizontalLine(centerX - x, centerX + x + horizontalOdd, centerY + y + verticalOdd));
                points.AddRange(DrawHorizontalLine(centerX - x, centerX + x + horizontalOdd, centerY - y));
            }
            else
            {
                points.Add(new System.Drawing.Point(centerX + x + horizontalOdd, centerY + y + verticalOdd)); //Dolní pravý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX - x, centerY + y + verticalOdd)); //Dolní levý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX + x + horizontalOdd, centerY - y)); //Horní pravý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX - x, centerY - y)); //Horní levý kvadrant kruhu
            }
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
    }
}
