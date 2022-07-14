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
                generatedPoints.AddRange(Interpolate(bitmap, currentPoint, previousPoint));
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
                interpolatedPoints.AddRange(Interpolate(bitmap, currentPoint, previousPoint));
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

        public void Line(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, bool alternativeFunction01, bool alternativeFunction02, bool previewBitmap = false)
        {
            List<System.Drawing.Point> generatedPoints = line.GeneratePoints(bitmap, startPoint, endPoint, alternativeFunction01, alternativeFunction02);
            foreach (System.Drawing.Point point in generatedPoints)
            {
                StrokeThicknessSetter(bitmap, point, color, alphaBlending, thickness, undoColors, undoPoints, previewBitmap);
            }
        }

        public void Ellipse(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, bool alternativeFunction01, bool alternativeFunction02, bool previewBitmap = false)
        {
            List<System.Drawing.Point> generatedPoints = ellipse.GeneratePoints(bitmap, startPoint, endPoint, alternativeFunction01, alternativeFunction02);
            foreach (System.Drawing.Point point in generatedPoints)
            {
                StrokeThicknessSetter(bitmap, point, color, alphaBlending, thickness, undoColors, undoPoints, previewBitmap);
            }
        }

        public void Rectangle(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, Color color, bool alphaBlending, int thickness, List<Color> undoColors, List<System.Drawing.Point> undoPoints, bool alternativeFunction01, bool alternativeFunction02, bool previewBitmap = false)
        {
            List<System.Drawing.Point> generatedPoints = rectangle.GeneratePoints(bitmap, startPoint, endPoint, alternativeFunction01, alternativeFunction02);
            foreach (System.Drawing.Point point in generatedPoints)
            {
                StrokeThicknessSetter(bitmap, point, color, alphaBlending, thickness, undoColors, undoPoints, previewBitmap);
            }
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
                interpolatedPoints.AddRange(Interpolate(bitmap, currentPoint, previousPoint));
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
                interpolatedPoints.AddRange(Interpolate(bitmap, currentPoint, previousPoint));
            }
            generatedPoints.AddRange(interpolatedPoints);
            shading.GeneratePoints(generatedPoints, bitmap, darken, strokeThickness, undoPoints, undoColors);
        }

        public List<System.Drawing.Point> Interpolate(WriteableBitmap bitmap, System.Drawing.Point currentPoint, System.Drawing.Point previousPoint)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            if ((Math.Abs(currentPoint.X - previousPoint.X) > 1) || (Math.Abs(currentPoint.Y - previousPoint.Y) > 1))
            {
                points.AddRange(line.GeneratePoints(bitmap, currentPoint, previousPoint, false, false));
            }
            return points;
        }
    }
}
