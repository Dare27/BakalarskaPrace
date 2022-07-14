using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class LineTool: IGeometricTool
    {
        public List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            if (alternativeFunction01 == false)
            {
                //Rozdíl mezi body na ose X a Y
                int dx = Math.Abs(endPoint.X - startPoint.X);
                int dy = Math.Abs(endPoint.Y - startPoint.Y);

                //Nalezení kvadrantu
                int sx = startPoint.X < endPoint.X ? 1 : -1;
                int sy = startPoint.Y < endPoint.Y ? 1 : -1;
                int error = dx - dy;

                while (true) 
                {
                    points.Add(new System.Drawing.Point(startPoint.X, startPoint.Y));
                    if (startPoint.X == endPoint.X && startPoint.Y == endPoint.Y) break;
                    int e2 = 2 * error;
                    if (e2 >= -dy) 
                    {
                        error = error - dy;
                        startPoint.X = startPoint.X + sx;
                    }

                    if (e2 <= dx) 
                    {
                        error = error + dx;
                        startPoint.Y = startPoint.Y + sy;
                    }
                }
            }
            else
            {
                //Rozdíl mezi body na ose X a Y
                int dx = Math.Abs(endPoint.X - startPoint.X) + 1;
                int dy = Math.Abs(endPoint.Y - startPoint.Y) + 1;

                //Nalezení kvadrantu
                int sx = startPoint.X < endPoint.X ? 1 : -1;
                int sy = startPoint.Y < endPoint.Y ? 1 : -1;

                //Kroky musí mít rovnoměrné rozdělení
                double ratio = Math.Max(dx, dy) / Math.Min(dx, dy);
                double pixelStep = Math.Round(ratio);

                //Nejdelší délka kroku je rovna nejdelší straně obrázku
                if (pixelStep > Math.Min(dx, dy)) pixelStep = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);

                int maxDistance = (int)Math.Sqrt((Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2)));
                int x = startPoint.X;
                int y = startPoint.Y;

                for (int i = 1; i <= maxDistance + 1; i++)
                {
                    points.Add(new System.Drawing.Point(x, y));

                    //Pokud se vzdálenost aktuálního bodu rovná maximální vzádelnosti tak ukončíme for cyklus
                    if (Math.Sqrt((Math.Pow(startPoint.X - x, 2) + Math.Pow(startPoint.Y - y, 2))) >= maxDistance)
                    {
                        break;
                    }

                    bool isAtStep;
                    if (i % pixelStep == 0) isAtStep = true;
                    else isAtStep = false;

                    if (dx >= dy || isAtStep)
                    {
                        x += sx;
                    }

                    if (dy >= dx || isAtStep)
                    {
                        y += sy;
                    }
                }
            }
            return points;
        }
    }
}
