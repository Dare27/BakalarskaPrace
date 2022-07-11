using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class Tools : ToolSettings
    {
        LineTool line = new LineTool();
        RectangleTool rectangle = new RectangleTool();
        EllipseTool ellipse = new EllipseTool();
        FloodFillTool floodFill = new FloodFillTool();
        ColorReplacementTool colorReplacement = new ColorReplacementTool();
        SymmetricTool symmetric = new SymmetricTool();
        DitheringTool dithering = new DitheringTool();
        ShadingTool shading = new ShadingTool();

        public List<System.Drawing.Point> DrawLine(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return line.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public List<System.Drawing.Point> DrawEllipse(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return ellipse.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public List<System.Drawing.Point> DrawRectangle(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return rectangle.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public List<System.Drawing.Point> DrawFloodFill(WriteableBitmap bitmap, System.Drawing.Point point)
        {
            return floodFill.GeneratePoints(bitmap, point);
        }

        public List<System.Drawing.Point> DrawColorReplacement(WriteableBitmap bitmap, System.Drawing.Point point)
        {
            return colorReplacement.GeneratePoints(bitmap, point);
        }

        public List<System.Drawing.Point> DrawSymmetric(WriteableBitmap bitmap, System.Drawing.Point point)
        {
            return symmetric.GeneratePoints(bitmap, point);
        }

        public void DrawDithering(List<System.Drawing.Point> points, Color color01, Color color02, WriteableBitmap bitmap, int strokeThickness, bool alphaBlending, List<System.Drawing.Point> visitedPoints, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            dithering.GeneratePoints(points, color01, color02, bitmap, strokeThickness, alphaBlending, visitedPoints, undoPoints, undoColors);
        }

        public void DrawShading(List<System.Drawing.Point> points, WriteableBitmap bitmap, bool darken, int strokeThickness, List<System.Drawing.Point> visitedPoints, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            shading.GeneratePoints(points, bitmap, darken, strokeThickness, visitedPoints, undoPoints, undoColors);
        }
    }
}
