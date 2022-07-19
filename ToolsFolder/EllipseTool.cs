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

            //Částečné posunutí 
            int horizontalOdd;
            if ((startPoint.X + endPoint.X) % 2 != 0) horizontalOdd = 1;
            else horizontalOdd = 0;

            int verticalOdd;
            if ((startPoint.Y + endPoint.Y) % 2 != 0) verticalOdd = 1;
            else verticalOdd = 0;

            int centerX = (startPoint.X + endPoint.X) / 2;
            int centerY = (startPoint.Y + endPoint.Y) / 2;
            int radX = centerX - Math.Min(startPoint.X, endPoint.X);
            int radY = centerY - Math.Min(startPoint.Y, endPoint.Y);
            int radX2 = radX * radX;
            int radY2 = radY * radY;
            int twoRadX2 = 2 * radX2;
            int twoRadY2 = 2 * radY2;
            int p;
            int x = 0;
            int y = radY;
            int px = 0;
            int py = twoRadX2 * y;
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

            //Počáteční rozhodovací parametr 2. regionu 
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
                    px += twoRadY2;
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
                points.Add(new System.Drawing.Point(centerX + x + horizontalOdd, centerY + y + verticalOdd));   //Dolní pravý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX - x, centerY + y + verticalOdd));                   //Dolní levý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX + x + horizontalOdd, centerY - y));                 //Horní pravý kvadrant kruhu
                points.Add(new System.Drawing.Point(centerX - x, centerY - y));                                 //Horní levý kvadrant kruhu
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
