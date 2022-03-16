using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;

namespace BakalarskaPrace
{
    internal class Geometry
    {
        //V případě této aplikace je nutné používat Mid-Point algoritmus, protože Bresenhaimův algoritmus nedosahuje při nízkých velikostech kruhu vzhledného výsledku
        public List<Point> DrawCircle(int centerX, int centerY, int rad, bool fill)
        {
            int x = rad, y = 0;
            List<Point> points = new List<Point>();

            // When radius is zero only a single point will be printed
            if (rad > 0)
            {
                if (fill)
                {
                    points.AddRange(DrawLine(centerX + x, centerY + y, centerX - rad, centerY + y));
                }
                else
                {
                    points.Add(new Point(centerX + x, centerY + y));
                    points.Add(new Point(x + centerX - rad, centerY - rad));
                    points.Add(new Point(y + centerX, x + centerY));
                    points.Add(new Point(-rad + centerX, x + centerY - rad));

                }
            }
            else
            {
                points.Add(new Point(centerX, centerY));
            }

            // Initialising the value of P
            int P = 1 - rad;
            while (x > y)
            {
                y++;

                // Mid-point is inside or on the perimeter
                if (P <= 0)
                    P = P + 2 * y + 1;

                // Mid-point is outside the perimeter
                else
                {
                    x--;
                    P = P + 2 * y - 2 * x + 1;
                }

                // All the perimeter points have already been printed
                if (x < y)
                    break;

                // Printing the generated point and its reflection in the other octants after translation
                points.AddRange(QuadrantPlotter(centerX, centerY, x, y, fill));

                // If the generated point is on the line x = y then the perimeter points have already been printed
                if (x != y)
                {
                    if (fill)
                    {
                        points.AddRange(DrawLine(y + centerX, x + centerY, -y + centerX, x + centerY));
                        points.AddRange(DrawLine(y + centerX, -x + centerY, -y + centerX, -x + centerY));
                    }
                    else
                    {
                        points.Add(new Point(y + centerX, x + centerY));
                        points.Add(new Point(-y + centerX, x + centerY));
                        points.Add(new Point(y + centerX, -x + centerY));
                        points.Add(new Point(-y + centerX, -x + centerY));
                    }
                }
            }
            return points;
        }

        public List<Point> DrawEllipse(int centerX, int centerY, int radX, int radY, bool fill)
        {
            int radX2 = radX * radX;
            int radY2 = radY * radY;
            int twoRadX2 = 2 * radX2;
            int twoRadY2 = 2 * radY2;
            int p;
            int x = 0;
            int y = radY;
            int px = 0;
            int py = twoRadX2 * y;
            List<Point> points = new List<Point>();

            // Plot the initial point in each quadrant
            points.AddRange(QuadrantPlotter(centerX, centerY, x, y, fill));

            // Initial decision parameter of region 1
            p = (int)Math.Round(radY2 - (radX2 * radY) + (0.25 * radX2));

            // Plotting points of region 1
            while (px < py)
            {
                x++;
                px += twoRadY2;
                // Checking and updating value of decision parameter based on algorithm
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
                points.AddRange(QuadrantPlotter(centerX, centerY, x, y, fill));
            }

            // Decision parameter of region 2
            p = (int)Math.Round(radY2 * (x + 0.5) * (x + 0.5) + radX2 * (y - 1) * (y - 1) - radX2 * radY2);

            // Plotting points of region 2
            while (y > 0)
            {
                y--;
                py -= twoRadX2;
                // Checking and updating parameter value based on algorithm
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
                points.AddRange(QuadrantPlotter(centerX, centerY, x, y, fill));
            }
            return points;
        }

        //Vykreslit symetrické body ve všech kvadrantech pomocí souřadnic
        public List<Point> QuadrantPlotter(int centerX, int centerY, int x, int y, bool fill)
        {
            List<Point> points = new List<Point>(); ;
            if (fill)
            {
                points.AddRange(DrawLine(centerX - x, centerY + y, centerX + x, centerY + y));
                points.AddRange(DrawLine(centerX - x, centerY - y, centerX + x, centerY - y));
            }
            else
            {
                points.Add(new Point(centerX + x, centerY + y));
                points.Add(new Point(centerX - x, centerY + y));
                points.Add(new Point(centerX + x, centerY - y));
                points.Add(new Point(centerX - x, centerY - y));
            }
            return points;
        }

        public List<Point> DrawRectangle(int x0, int y0, int x1, int y1, bool fill)
        {
            List<Point> points = new List<Point>();
            if (y0 < y1)
            {
                for (int y = y0; y < y1; y++)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawLine(x0, y, x1, y);
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
                for (int y = y0; y > y1; y--)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawLine(x0, y, x1, y);
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
                for (int x = x0; x < x1; x++)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawLine(x, y0, x, y1);
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
                for (int x = x0; x > x1; x--)
                {
                    if (fill)
                    {
                        List<Point> fillPoints = DrawLine(x, y0, x, y1);
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

        //Bresenhaimův algoritmus pro kreslení přímek
        public List<Point> DrawLine(int x, int y, int x2, int y2)
        {
            List<Point> points = new List<Point>();
            int w = x2 - x;
            int h = y2 - y;
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
                points.Add(new Point(x, y));
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }
            return points;
        }

        public List<Point> DrawStraightLine(int x0, int y0, int x1, int y1, int imageWidth, int imageHeight)
        {
            int dx = Math.Abs(x1 - x0) + 1;
            int dy = Math.Abs(y1 - y0) + 1;

            //Kroky musí mít rovnoměrné rozdělení
            double ratio = Math.Max(dx, dy) / Math.Min(dx, dy);
            double pixelStep = Math.Round(ratio);

            if (pixelStep > Math.Min(dx, dy))
            {
                pixelStep = Math.Max(imageWidth, imageHeight);
            }

            int maxDistance = (int)Math.Sqrt((Math.Pow(x0 - x1, 2) + Math.Pow(y0 - y1, 2)));
            int x = x0;
            int y = y0;

            List<Point> points = new List<Point>();

            for (int i = 1; i <= maxDistance + 1; i++)
            {
                if (!points.Contains(new Point(x, y)))
                {
                    points.Add(new Point(x, y));
                }

                if (Math.Sqrt((Math.Pow(x0 - x, 2) + Math.Pow(y0 - y, 2))) >= maxDistance)
                {
                    break;
                }

                bool isAtStep;

                if (i % pixelStep == 0)
                {
                    isAtStep = true;
                }
                else
                {
                    isAtStep = false;
                }

                if (dx >= dy || isAtStep)
                {
                    if (x0 < x1)
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
                    if (y0 < y1)
                    {
                        y += 1;
                    }
                    else
                    {
                        y -= 1;
                    }
                }
            }
            return points;
        }
    }
}
