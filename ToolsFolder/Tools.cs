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

        public void DefualtBrush(WriteableBitmap bitmap, System.Drawing.Point currentPoint, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, System.Drawing.Point previousPoint = new System.Drawing.Point()) 
        {
            List<System.Drawing.Point> generatedPoints = new List<System.Drawing.Point>() { currentPoint };
            if (!previousPoint.IsEmpty) 
            {
                generatedPoints.AddRange(Interpolate(currentPoint, previousPoint, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            foreach (System.Drawing.Point point in generatedPoints) 
            {
                StrokeThicknessSetter(bitmap, point, color, alphaBlending, thickness, undoColors, undoPoints);
            }
        }

        public void Symmetric(WriteableBitmap bitmap, System.Drawing.Point currentPoint, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, System.Drawing.Point previousPoint = new System.Drawing.Point())
        {
            List<System.Drawing.Point> generatedPoints = symmetric.GeneratePoints(bitmap, currentPoint);
            List<System.Drawing.Point> interpolatedPoints = new List<System.Drawing.Point>();
            if (!previousPoint.IsEmpty)
            {
                interpolatedPoints.AddRange(Interpolate(currentPoint, previousPoint, bitmap.PixelWidth, bitmap.PixelHeight));
                generatedPoints.AddRange(interpolatedPoints);
            }

            foreach (System.Drawing.Point point in interpolatedPoints)
            {
                generatedPoints.AddRange(symmetric.GeneratePoints(bitmap, point));
            }

            foreach (System.Drawing.Point point in generatedPoints)
            {
                StrokeThicknessSetter(bitmap, point, color, alphaBlending, thickness, undoColors, undoPoints);
            }
        }

        public List<System.Drawing.Point> Line(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return line.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public List<System.Drawing.Point> Ellipse(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return ellipse.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public List<System.Drawing.Point> Rectangle(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height)
        {
            return rectangle.GeneratePoints(startPoint, endPoint, alternativeFunction01, alternativeFunction02, width, height);
        }

        public void FloodFill(WriteableBitmap bitmap, System.Drawing.Point point, Color color, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            floodFill.GeneratePoints(bitmap, point, color, undoPoints, undoColors);
        }

        public void ColorReplacement(WriteableBitmap bitmap, System.Drawing.Point point, Color color, List<System.Drawing.Point> undoPoints, List<Color> undoColors)
        {
            colorReplacement.GeneratePoints(bitmap, point, color, undoPoints, undoColors);
        }

        public void Dithering(WriteableBitmap bitmap, System.Drawing.Point currentPoint, Color color01, Color color02, int strokeThickness, bool alphaBlending, List<System.Drawing.Point> undoPoints, List<Color> undoColors, System.Drawing.Point previousPoint = new System.Drawing.Point())
        {
            List<System.Drawing.Point> generatedPoints = new List<System.Drawing.Point>() { currentPoint };
            List<System.Drawing.Point> interpolatedPoints = new List<System.Drawing.Point>();
            if (!previousPoint.IsEmpty)
            {
                interpolatedPoints.AddRange(Interpolate(currentPoint, previousPoint, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            generatedPoints.AddRange(interpolatedPoints);
            dithering.GeneratePoints(generatedPoints, color01, color02, bitmap, strokeThickness, alphaBlending, undoPoints, undoColors);
        }

        public void Shading(WriteableBitmap bitmap, System.Drawing.Point currentPoint, bool darken, int strokeThickness, List<System.Drawing.Point> undoPoints, List<Color> undoColors, System.Drawing.Point previousPoint = new System.Drawing.Point())
        {
            List<System.Drawing.Point> generatedPoints = new List<System.Drawing.Point>() { currentPoint };
            List<System.Drawing.Point> interpolatedPoints = new List<System.Drawing.Point>();
            if (!previousPoint.IsEmpty)
            {
                interpolatedPoints.AddRange(Interpolate(currentPoint, previousPoint, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            generatedPoints.AddRange(interpolatedPoints);
            shading.GeneratePoints(generatedPoints, bitmap, darken, strokeThickness, undoPoints, undoColors);
        }

        public List<System.Drawing.Point> Interpolate(System.Drawing.Point currentPoint, System.Drawing.Point previousPoint, int width, int height)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            if ((Math.Abs(currentPoint.X - previousPoint.X) > 1) || (Math.Abs(currentPoint.Y - previousPoint.Y) > 1))
            {
                points.AddRange(Line(currentPoint, previousPoint, false, false, width, height));
            }
            return points;
        }
    }
}
