using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakalarskaPrace.ToolsFolder
{
    internal class LineTool: IGeometricTool
    {
        public List<System.Drawing.Point> GeneratePoints(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            if (alternativeFunction01 == false)
            {
                int w = endPoint.X - startPoint.X;
                int h = endPoint.Y - startPoint.Y;
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
                    points.Add(new System.Drawing.Point(startPoint.X, startPoint.Y));
                    numerator += shortest;
                    if (!(numerator < longest))
                    {
                        numerator -= longest;
                        startPoint.X += dx1;
                        startPoint.Y += dy1;
                    }
                    else
                    {
                        startPoint.X += dx2;
                        startPoint.Y += dy2;
                    }
                }
            }
            else
            {
                int dx = Math.Abs(startPoint.X - endPoint.X) + 1;
                int dy = Math.Abs(startPoint.Y - endPoint.Y) + 1;

                //Kroky musí mít rovnoměrné rozdělení
                double ratio = Math.Max(dx, dy) / Math.Min(dx, dy);
                double pixelStep = Math.Round(ratio);

                //Nejdelší délka kroku je rovna nejdelší straně obrázku
                if (pixelStep > Math.Min(dx, dy)) pixelStep = Math.Max(width, height);

                int maxDistance = (int)Math.Sqrt((Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2)));
                int x = endPoint.X;
                int y = endPoint.Y;

                for (int i = 1; i <= maxDistance + 1; i++)
                {
                    if (!points.Contains(new System.Drawing.Point(x, y)))
                    {
                        points.Add(new System.Drawing.Point(x, y));
                    }

                    if (Math.Sqrt((Math.Pow(endPoint.X - x, 2) + Math.Pow(endPoint.Y - y, 2))) >= maxDistance)
                    {
                        break;
                    }

                    bool isAtStep;
                    if (i % pixelStep == 0) isAtStep = true;
                    else isAtStep = false;

                    if (dx >= dy || isAtStep)
                    {
                        if (endPoint.X < startPoint.X)
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
                        if (endPoint.Y < startPoint.Y)
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
    }
}
