using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;

namespace BakalarskaPrace
{
    internal class Geometry
    {
        //V případě této aplikace je nutné používat Mid-Point algoritmus, protože Bresenhaimův algoritmus nedosahuje při nízkých velikostech kruhu vzhledného výsledku
        public List<Point> DrawEllipse(Point startPos, Point endPos, bool fill, bool circle)
        {
            int x0 = (int)startPos.X;
            int y0 = (int)startPos.Y;
            int x1 = (int)endPos.X;
            int y1 = (int)endPos.Y;

            if (circle)
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
            if ((x0 + x1) % 2 != 0)
                horizontalOdd = 1;
            else
                horizontalOdd = 0;

            int verticalOdd;
            if ((y0 + y1) % 2 != 0)
                verticalOdd = 1;
            else
                verticalOdd = 0;

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
            List<Point> points = new List<Point>();

            //Vykreslení počátečního bodu do každého kvadrantu
            points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, fill, verticalOdd, horizontalOdd));

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
                points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, fill, verticalOdd, horizontalOdd));
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
                points.AddRange(QuadrantPlotter((int)centerX, (int)centerY, (int)x, (int)y, fill, verticalOdd, horizontalOdd));
            }
            return points;
        }

        //Vykreslit symetrické body ve všech kvadrantech pomocí souřadnic
        public List<Point> QuadrantPlotter(int centerX, int centerY, int x, int y, bool fill, int verticalOdd, int horizontalOdd)
        {
            List<Point> points = new List<Point>(); ;
            if (fill)
            {
                points.AddRange(DrawHorizontalLine(centerX - x, centerX + x + horizontalOdd, centerY + y + verticalOdd));
                points.AddRange(DrawHorizontalLine(centerX - x, centerX + x + horizontalOdd, centerY - y));
            }
            else
            {
                points.Add(new Point(centerX + x + horizontalOdd, centerY + y + verticalOdd)); //Dolní pravý kvadrant kruhu
                points.Add(new Point(centerX - x, centerY + y + verticalOdd)); //Dolní levý kvadrant kruhu
                points.Add(new Point(centerX + x + horizontalOdd, centerY - y)); //Horní pravý kvadrant kruhu
                points.Add(new Point(centerX - x, centerY - y)); //Horní levý kvadrant kruhu
            }
            return points;
        }

        public List<Point> DrawRectangle(Point startPos, Point endPos, bool square, bool fill)
        {
            List<Point> points = new List<Point>();
            int x0 = (int)startPos.X;
            int y0 = (int)startPos.Y;
            int x1 = (int)endPos.X;
            int y1 = (int)endPos.Y;

            if (square) 
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
                    if (fill)
                    {
                        List<Point> fillPoints = DrawHorizontalLine(x1, x0, y);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new Point(x0, y));
                        points.Add(new Point(x1, y));
                    }
                }
            }
            else
            {
                for (int y = y0; y >= y1; y--)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawHorizontalLine(x0, x1, y);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new Point(x0, y));
                        points.Add(new Point(x1, y));
                    }
                }
            }

            if (x0 < x1)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawVerticalLine(y0, y1, x);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new Point(x, y0));
                        points.Add(new Point(x, y1));
                    }
                }
            }
            else
            {
                for (int x = x0; x >= x1; x--)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawVerticalLine(y1, y0, x);
                        points.AddRange(fillPoints);
                    }
                    else
                    {
                        points.Add(new Point(x, y0));
                        points.Add(new Point(x, y1));

                    }
                }
            }
            points.Add(new Point(x1, y1));
            return points;
        }

        public List<Point> DrawHorizontalLine(int x0, int x1, int y) 
        {
            List<Point> points = new List<Point>();
            for (int x = x0; x <= x1; x++)
            {
                points.Add(new Point(x, y));
            }
            return points;
        }

        public List<Point> DrawVerticalLine(int y0, int y1, int x)
        {
            List<Point> points = new List<Point>();
            for (int y = y0; y <= y1; y++)
            {
                points.Add(new Point(x, y));
            }
            return points;
        }

        //Bresenhaimův algoritmus pro kreslení přímek
        public List<Point> DrawLine(int x0, int y0, int x1, int y1, int imageWidth, int imageHeight, bool straight)
        {
            List<Point> points = new List<Point>();

            if (straight == false)
            {
                int w = x1 - x0;
                int h = y1 - y0;
                int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

                //Nalezení kvadrantu
                if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
                if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
                if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
                int longest = Math.Abs(w);
                int shortest = Math.Abs(h);

                if (!(longest > shortest))
                {
                    longest = Math.Abs(h);
                    shortest = Math.Abs(w);
                    if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                    dx2 = 0;
                }

                int numerator = longest >> 1;
                for (int i = 0; i <= longest; i++)
                {
                    points.Add(new Point(x0, y0));
                    numerator += shortest;
                    if (!(numerator < longest))
                    {
                        numerator -= longest;
                        x0 += dx1;
                        y0 += dy1;
                    }
                    else
                    {
                        x0 += dx2;
                        y0 += dy2;
                    }
                }
            }
            else 
            {
                int dx = Math.Abs(x0 - x1) + 1;
                int dy = Math.Abs(y0 - y1) + 1;

                //Kroky musí mít rovnoměrné rozdělení
                double ratio = Math.Max(dx, dy) / Math.Min(dx, dy);
                double pixelStep = Math.Round(ratio);

                //Nejdelší délka kroku je rovna nejdelší straně obrázku
                if (pixelStep > Math.Min(dx, dy)) pixelStep = Math.Max(imageWidth, imageHeight);

                int maxDistance = (int)Math.Sqrt((Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2)));
                int x = x1;
                int y = y1;

                for (int i = 1; i <= maxDistance + 1; i++)
                {
                    if (!points.Contains(new Point(x, y)))
                    {
                        points.Add(new Point(x, y));
                    }

                    if (Math.Sqrt((Math.Pow(x1 - x, 2) + Math.Pow(y1 - y, 2))) >= maxDistance)
                    {
                        break;
                    }

                    bool isAtStep;
                    if (i % pixelStep == 0) isAtStep = true;
                    else isAtStep = false;

                    if (dx >= dy || isAtStep)
                    {
                        if (x1 < x0)
                        {
                            x += 1;
                        }
                        else
                        {
                            x -= 1;
                        }
                    }

                    if (dy >= dx || isAtStep)
                    {
                        if (y1 < y0)
                        {
                            y += 1;
                        }
                        else
                        {
                            y -= 1;
                        }
                    }
                }
            }
            return points;
        }

        public List<Point> Interpolate(int currentPointX, int currentPointY, int previousPointX, int previousPointY, int width, int height)
        {
            List<Point> points = new List<Point>();
            if ((Math.Abs(currentPointX - previousPointX) > 1) || (Math.Abs(currentPointY - previousPointY) > 1))
            {
                points.AddRange(DrawLine(currentPointX, currentPointY, previousPointX, previousPointY, width, height, false));
            }
            return points;
        }
    }
}
