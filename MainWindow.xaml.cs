using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap currentBitmap;
        int currentBitmapIndex = 0;
        int currentLayerIndex = 0;
        private List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
        private List<List<WriteableBitmap>> layers = new List<List<WriteableBitmap>>();
        private List<System.Windows.Controls.Button> previewButtons = new List<System.Windows.Controls.Button>();
        private List<System.Windows.Controls.Button> layerButtons = new List<System.Windows.Controls.Button>();
        WriteableBitmap previewBitmap;
        Color[] currentColors;
        List<Color> colorPalette = new List<Color>();
        int currentColorIndex = 0;
        int strokeThickness = 1;
        bool alphaBlending;
        Color seedColor;
        bool shadingValue;
        enum ToolSelection { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipse, shading, rectangle, dithering, move, path, rectangleSelection, lassoSelection, shapeSelection };
        ToolSelection currentTool = ToolSelection.brush;
        Point gridDragStartPoint;
        System.Windows.Vector gridDragOffset;
        int width, height;
        double currentScale = 1.0;
        Point mousePosition = new Point(0, 0);
        Point mouseDownPosition = new Point(-1, -1);
        Point previewMousePosition = new Point(0, 0);
        List<Point> visitedPoints = new List<Point>();
        List<Point> previewMousePoints = new List<Point>();
        List<Point> drawPoints = new List<Point>();
        List<Point> undoPoints = new List<Point>();
        List<Color> undoColors = new List<Color>();
        List<Point> clipboardPoints = new List<Point>();
        Point previousMousePoint = new Point();
        bool selection;

        bool onionSkinning;
        System.Windows.Controls.Button lastToolButton;
        System.Windows.Controls.Button currentColorLeftButton;

        private Timer timer = new Timer();
        int timerInterval = 1000;
        int currentAnimationIndex;
        int currentFPSTarget = 12;
        bool playAnimation = true;

        ImageManipulation imageManipulation;
        Geometry geometry;
        Transform transform;
        Tools tools;
        Filters filters;

        private Stack<Action> undoStack = new Stack<Action>();
        private Stack<Action> redoStack = new Stack<Action>();

        public MainWindow()
        {
            imageManipulation = new ImageManipulation();
            geometry = new Geometry();
            transform = new Transform();
            tools = new Tools();
            filters = new Filters();

            layers.Add(bitmaps); //Výchozí vrstva

            InitializeComponent();
            this.Show();
            this.StateChanged += new EventHandler(Window_StateChanged);
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
            paintSurface.Visibility = Visibility.Hidden;
            WindowStartup windowStartup = new WindowStartup();
            windowStartup.ShowDialog();

            currentColors = new Color[4];
            currentColors[0] = colorSelector.SelectedColor;
            currentColors[1] = colorSelector.SecondaryColor;
            currentColors[2] = Color.FromArgb(255, 178, 213, 226);
            currentColors[3] = Color.FromArgb(0, 0, 0, 0);

            paintSurface.Visibility = Visibility.Visible;
            width = windowStartup.newWidth;
            height = windowStartup.newHeight;

            previewBitmap = BitmapFactory.New(width, height);
            previewImage.Source = previewBitmap;
            previewImage.Width = width;
            previewImage.Height = height;

            currentBitmap = BitmapFactory.New(width, height);
            bitmaps.Add(currentBitmap);
            paintSurface.Width = width;
            paintSurface.Height = height;
            image.Width = width;
            image.Height = height;
            image.Source = bitmaps[currentBitmapIndex];
            Action action = () => transform.Resize(bitmaps, windowStartup.newWidth, windowStartup.newHeight, "middle");
            //AddActionToUndo(action, false, false);
            LabelPosition.Content = "[" + width + ":" + height + "] " + 0 + ":" + 0;
            LabelScale.Content = "1.0";
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            lastToolButton = brush;
            UpdateLayerPreviewButtons();
            UpdateImagePreviewButtons();
            Center();

            timerInterval = 1000 / currentFPSTarget;
            timer.Interval = timerInterval;
            timer.Enabled = true;
            timer.Tick += new System.EventHandler(OnTimedEvent);
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            if (currentAnimationIndex + 1 < bitmaps.Count) currentAnimationIndex += 1;
            else currentAnimationIndex = 0;
            animationPreview.Source = bitmaps[currentAnimationIndex];
        }

        private unsafe void Image_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            currentColors[1] = colorSelector.SecondaryColor;
            currentColors[0] = colorSelector.SelectedColor;
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;
            mousePosition = new Point(x, y);

            drawPoints = new List<Point>();
            undoPoints = new List<Point>();
            undoColors = new List<Color>();
            visitedPoints = new List<Point>();

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                currentColorIndex = colorIndex;

                switch (currentTool)
                {
                    case ToolSelection.brush:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            previousMousePoint = new Point(x, y);
                            GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, strokeThickness);
                            break;
                        }
                    case ToolSelection.symmetricBrush:
                        {
                            drawPoints = tools.SymmetricDrawing(x, y, currentBitmap);
                            GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, strokeThickness);
                            break;
                        }
                    case ToolSelection.eraser:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            GeneratePoints(drawPoints, new List<Color>() { currentColors[3] }, currentBitmapIndex, false, strokeThickness);
                            break;
                        }
                    case ToolSelection.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case ToolSelection.bucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            undoColors.Add(seedColor);
                            drawPoints = tools.FloodFill(currentBitmap, new Point(x, y), seedColor, currentColors[colorIndex], alphaBlending);
                            undoPoints = drawPoints;
                            GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, 1);
                            break;
                        }
                    case ToolSelection.specialBucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            undoColors.Add(seedColor);
                            drawPoints = tools.SpecialBucket(currentBitmap, currentColors[colorIndex], seedColor, alphaBlending);
                            undoPoints = drawPoints;
                            GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, false, 1);
                            break;
                        }
                    case ToolSelection.line:
                        {
                            mouseDownPosition = new Point(x, y);
                            break;
                        }
                    case ToolSelection.path:
                        {
                            if (mouseDownPosition == new Point(-1, -1))
                                mouseDownPosition = new Point(x, y);
                            break;
                        }
                    case ToolSelection.ellipse:
                        {
                            mouseDownPosition = new Point(x, y);
                            break;
                        }
                    case ToolSelection.rectangle:
                        {
                            mouseDownPosition = new Point(x, y);
                            break;
                        }
                    case ToolSelection.dithering:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            tools.Dithering(new List<Point>() { new Point(x, y) }, currentColors[0], currentColors[1], currentBitmap, strokeThickness, alphaBlending, visitedPoints, undoPoints, undoColors);
                            break;
                        }
                    case ToolSelection.shading:
                        {
                            shadingValue = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? false : true;
                            drawPoints = new List<Point>() { new Point(x, y) };
                            tools.Shading(new List<Point>() { new Point(x, y) }, currentBitmap, shadingValue, strokeThickness, visitedPoints, undoPoints, undoColors);
                            break;
                        }
                    case ToolSelection.rectangleSelection:
                        {
                            selection = !selection;
                            if (selection == true)
                            {
                                mouseDownPosition = new Point(x, y);
                                previewBitmap.Clear();
                                previewMousePoints.Clear();
                                previewMousePosition = new Point(x, y);
                            }
                            break;
                        }
                    default: break;
                }
            }
        }

        private unsafe void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;
            mousePosition = new Point(x, y);

            bool shift = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? true : false;
            bool ctrl = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? true : false;

            LabelPosition.Content = "[" + width + ":" + height + "] " + x + ":" + y;
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
            if (x >= 0 && y >= 0 && x < width && y < height && (previewMousePosition.X != x || previewMousePosition.Y != y))
            {
                if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                {
                    switch (currentTool)
                    {
                        case ToolSelection.brush:
                            {
                                Point currentMousePoint = new Point(x, y);
                                drawPoints.Add(currentMousePoint);
                                if ((Math.Sqrt(Math.Pow(currentMousePoint.X - previousMousePoint.X, 2)) > 1) || (Math.Sqrt(Math.Pow(currentMousePoint.Y - previousMousePoint.Y, 2)) > 1)) 
                                {
                                    Console.WriteLine("line");
                                }
                                previousMousePoint = currentMousePoint;

                                GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, strokeThickness);
                                break;
                            }
                        case ToolSelection.symmetricBrush:
                            {
                                drawPoints.AddRange(tools.SymmetricDrawing(x, y, currentBitmap));
                                GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, strokeThickness);
                                break;
                            }
                        case ToolSelection.eraser:
                            {
                                drawPoints.Add(new Point(x, y));
                                GeneratePoints(drawPoints, new List<Color>() { currentColors[3] }, currentBitmapIndex, false, strokeThickness);
                                break;
                            }
                        case ToolSelection.colorPicker:
                            {
                                ColorPicker(x, y, colorIndex);
                                break;
                            }
                        case ToolSelection.bucket:
                            {
                                seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                                undoColors.Add(seedColor);
                                drawPoints = new List<Point>();
                                drawPoints = tools.FloodFill(currentBitmap, new Point(x, y), seedColor, currentColors[colorIndex], alphaBlending);
                                undoPoints = drawPoints;
                                GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, alphaBlending, 1);
                                break;
                            }
                        case ToolSelection.specialBucket:
                            {
                                seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                                drawPoints = new List<Point>();
                                drawPoints = tools.SpecialBucket(currentBitmap, currentColors[colorIndex], seedColor, alphaBlending);
                                undoPoints = drawPoints;
                                GeneratePoints(drawPoints, new List<Color>() { currentColors[colorIndex] }, currentBitmapIndex, false, 1);
                                break;
                            }
                        case ToolSelection.dithering:
                            {
                                drawPoints.Add(new Point(x, y));
                                tools.Dithering(new List<Point>() { new Point(x, y) }, currentColors[0], currentColors[1], currentBitmap, strokeThickness, alphaBlending, visitedPoints, undoPoints, undoColors);
                                break;
                            }
                        case ToolSelection.shading:
                            {
                                shadingValue = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? false : true;
                                drawPoints.Add(new Point(x, y));
                                tools.Shading(new List<Point>() { new Point(x, y) }, currentBitmap, shadingValue, strokeThickness, visitedPoints, undoPoints, undoColors);
                                break;
                            }
                        case ToolSelection.rectangleSelection:
                            {

                                break;
                            }
                        default: break;
                    }
                }

                if (selection == false || (currentTool == ToolSelection.rectangleSelection && e.LeftButton == MouseButtonState.Pressed) || (currentTool == ToolSelection.rectangleSelection && e.LeftButton == MouseButtonState.Pressed))
                {
                    previewBitmap.Clear();
                    previewMousePoints.Clear();
                    previewMousePosition = new Point(x, y);
                }

                switch (currentTool)
                {
                    case ToolSelection.line:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && (int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                            {
                                previewMousePoints.AddRange(geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y, width, height, ctrl));
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case ToolSelection.path:
                        {
                            if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                            {
                                //Pokud uživatel drží ctrl začne se nová cesta
                                if (ctrl)
                                    previewMousePoints.Add(previewMousePosition);
                                else
                                    previewMousePoints.AddRange(geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y, width, height, false));
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case ToolSelection.ellipse:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && (int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                            {
                                previewMousePoints.AddRange(geometry.DrawEllipse(mouseDownPosition, e.GetPosition(image), shift, ctrl));
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case ToolSelection.rectangle:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && (int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                            {
                                previewMousePoints.AddRange(geometry.DrawRectangle(mouseDownPosition, e.GetPosition(image), ctrl, shift));
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case ToolSelection.rectangleSelection:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && (int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                previewMousePoints.AddRange(geometry.DrawRectangle(mouseDownPosition, e.GetPosition(image), false, true));
                            else
                                if (selection == false) previewMousePoints.Add(previewMousePosition);
                            break;
                        }
                    default:
                        previewMousePoints.Add(previewMousePosition);
                        break;
                }

                using (previewBitmap.GetBitmapContext())
                {
                    int previewStrokeThickness = (currentTool == ToolSelection.specialBucket || currentTool == ToolSelection.bucket || currentTool == ToolSelection.colorPicker) ? 1 : strokeThickness;
                    foreach (Point point in previewMousePoints)
                    {
                        StrokeThicknessSetter((int)point.X, (int)point.Y, currentColors[2], false, previewBitmap, previewStrokeThickness);
                    }
                }
            }
        }

        private unsafe void Image_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;
            mousePosition = new Point(x, y);

            visitedPoints.Clear();

            //Musí být definována nová proměnná jinak delegát předá odkaz na aktuální 
            int thickness = strokeThickness;
            Color color = new Color();
            color = Color.FromArgb(currentColors[currentColorIndex].A, currentColors[currentColorIndex].R, currentColors[currentColorIndex].G, currentColors[currentColorIndex].B);
            int index = currentBitmapIndex;

            switch (currentTool)
            {
                case ToolSelection.line:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, index, alphaBlending, thickness);
                        break;
                    }
                case ToolSelection.path:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, index, alphaBlending, thickness);
                        break;
                    }
                case ToolSelection.ellipse:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, index, alphaBlending, thickness);
                        break;
                    }
                case ToolSelection.rectangle:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, index, alphaBlending, thickness);
                        break;
                    }
                case ToolSelection.rectangleSelection:
                    {

                        break;
                    }
                default: break;
            }

            if (currentTool != ToolSelection.rectangleSelection)
            {
                List<Point> points = new List<Point>(undoPoints);
                List<Color> colors = new List<Color>(undoColors);
                List<WriteableBitmap> currentBitmaps = new List<WriteableBitmap>(bitmaps);
                Action action = () => GeneratePoints(points, colors, index, false, 1, true);
                AddActionToUndo(action, false, true);

                drawPoints = new List<Point>();
                undoPoints = new List<Point>();
                undoColors = new List<Color>();
                visitedPoints = new List<Point>();

                if (currentTool != ToolSelection.path) mouseDownPosition = new Point(-1, -1);
                else mouseDownPosition = new Point(x, y);

                previewBitmap.Clear();
                previewMousePoints.Clear();
                previewMousePoints.Add(mousePosition);
                using (previewBitmap.GetBitmapContext())
                {
                    int previewStrokeThickness = (currentTool == ToolSelection.specialBucket || currentTool == ToolSelection.bucket || currentTool == ToolSelection.colorPicker) ? 1 : strokeThickness;
                    foreach (Point point in previewMousePoints) 
                    {
                        StrokeThicknessSetter((int)point.X, (int)point.Y, currentColors[2], false, previewBitmap, previewStrokeThickness);
                    }
                }
            }
        }

        private void GeneratePoints(List<Point> points, List<Color> colors, int bitmapIndex, bool alphaBlend, int thickness, bool generateInverseAction = false, bool generateAction = false)
        {
            //Pokud se nezobrazuje aktuální obrázek při undo/redo tak ho zobrazit
            UpdateCurrentBitmap(bitmapIndex);

            if (generateInverseAction == true)
            {
                List<Point> redoPoints = new List<Point>(points);
                List<Color> redoColors = new List<Color>();

                foreach (var point in redoPoints)
                {
                    Color redoColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmaps[bitmapIndex]);
                    redoColors.Add(redoColor);
                }
                Action action = () => GeneratePoints(redoPoints, redoColors, bitmapIndex, false, 1, false, true);
                AddActionToRedo(action);
            }
            else if (generateAction == true) 
            {
                List<Point> undoPoints = new List<Point>(points);
                List<Color> undoColors = new List<Color>();

                foreach (var point in undoPoints)
                {
                    Color undoColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmaps[bitmapIndex]);
                    undoColors.Add(undoColor);
                }
                Action action = () => GeneratePoints(undoPoints, undoColors, bitmapIndex, false, 1, true, false);
                AddActionToUndo(action, false, false);
            }
            
            for (int i = 0; i < points.Count; i++)
            {
                if (colors.Count != 1 && colors.Count == points.Count)
                {
                    StrokeThicknessSetter((int)points[i].X, (int)points[i].Y, colors[i], alphaBlend, bitmaps[bitmapIndex], thickness);
                }
                else
                {
                    StrokeThicknessSetter((int)points[i].X, (int)points[i].Y, colors[0], alphaBlend, bitmaps[bitmapIndex], thickness);
                }
            }
        }

        //Brush settings
        public List<Point> StrokeThicknessSetter(int x, int y, Color color, bool AlphaBlend, WriteableBitmap bitmap, int thickness)
        {
            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            List<Point> points = new List<Point>();

            int size = thickness / 2;
            int isOdd = 0;

            if (thickness % 2 != 0) isOdd = 1;

            for (int i = -size; i < size + isOdd; i++)
            {
                for (int j = -size; j < size + isOdd; j++)
                {
                    // zkontrolovat jestli se pixel vejde do bitmapy
                    if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                    {
                        Point point = new Point(x + i, y + j);

                        //Pokud se zapisuje do preview bitmapy tak kontrola navštívených bodů vede k smazání bodů 
                        if (bitmap == previewBitmap)
                        {
                            points.Add(point);
                        }
                        else
                        {
                            if (!visitedPoints.Contains(point))
                            {
                                visitedPoints.Add(point);
                                points.Add(point);
                            }
                        }
                    }
                }
            }

            using (bitmap.GetBitmapContext())
            {
                foreach (var point in points)
                {
                    if (bitmap != previewBitmap)
                    {
                        Color colord = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmap);
                        undoColors.Add(colord);
                        undoPoints.Add(point);
                    }

                    Color finalColor = color;
                    Color currentPixelColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmap);
                    if (AlphaBlend == true) finalColor = imageManipulation.ColorMix(color, currentPixelColor);
                    imageManipulation.AddPixel((int)point.X, (int)point.Y, finalColor, bitmap);
                }
            }
            return points;
        }

        private void ColorPicker(int x, int y, int colorIndex)
        {
            currentColors[colorIndex] = imageManipulation.GetPixelColor(x, y, currentBitmap);
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = currentColors[colorIndex];
            if (colorIndex == 0) colorSelector.SelectedColor = currentColors[0];
            else colorSelector.SecondaryColor = currentColors[1];
        }

        private void ColorChanged(object sender, RoutedEventArgs e)
        {
            currentColors[1] = colorSelector.SecondaryColor;
            currentColors[0] = colorSelector.SelectedColor;
        }

        private void BrushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            strokeThickness = (int)e.NewValue;
        }

        private void AlphaBlending_Changed(object sender, RoutedEventArgs e)
        {
            alphaBlending = !alphaBlending;
        }

        //Tool buttons
        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = new System.Windows.Controls.Button();
            string text = ((System.Windows.Controls.Button)sender).Name;
            currentTool = (ToolSelection)Enum.Parse(typeof(ToolSelection), text);
            button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            if (lastToolButton != null) lastToolButton.IsEnabled = true;
            lastToolButton = button;
        }

        //Transform buttons
        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;
            bool horizontal = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? false : true;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) 
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < bitmaps.Count; i++) 
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            Flip(selectedBitmapIndexes, bitmaps, horizontal);
            Action action = () => Flip(selectedBitmapIndexes, bitmaps, horizontal, true);
            AddActionToUndo(action, false, true);
        }

        private void Flip(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool horizontal, bool generateInverseAction = false, bool generateAction = false) 
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => Flip(selectedBitmapIndexes, bitmaps, horizontal, false, true);
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => Flip(selectedBitmapIndexes, bitmaps, horizontal, true, false);
                AddActionToUndo(action, false, false);
            }
            
            transform.Flip(selectedBitmapIndexes, bitmaps, horizontal);
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;
            bool clockwise = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? true : false;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            Rotate(selectedBitmapIndexes, bitmaps, clockwise);
            Action action = () => Rotate(selectedBitmapIndexes, bitmaps, !clockwise, true);
            AddActionToUndo(action, false, true);
        }

        private void Rotate(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool clockwise, bool generateInverseAction = false, bool generateAction = false) 
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => Rotate(selectedBitmapIndexes, bitmaps, !clockwise, false, true);
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => Rotate(selectedBitmapIndexes, bitmaps, !clockwise, true, false);
                AddActionToUndo(action, false, false);
            }

            transform.RotateImage(selectedBitmapIndexes, bitmaps, clockwise);
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
        }

        private void CropToFit_Click(object sender, RoutedEventArgs e)
        {
            List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
            Action originalAction = () => CropToFit(true);
            Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
            AddActionToUndo(action, false, true);
            CropToFit();
        }

        private void CropToFit(bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
                Action originalAction = () => CropToFit(true);
                Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
                AddActionToUndo(action, false, false);
            }

            transform.CropToFit(bitmaps);
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
            Center();
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            WindowResize subwindow = new WindowResize();
            subwindow.ShowDialog();

            if (subwindow.newWidth != 0 && subwindow.newHeight != 0)
            {
                int newWidth = subwindow.newWidth;
                int newHeight = subwindow.newHeight;
                string position = subwindow.position;

                List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
                Action originalAction = () => Resize(newWidth, newHeight, position, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
                AddActionToUndo(action, false, true);
                Resize(newWidth, newHeight, position);
            }
        }

        private void Resize(int newWidth, int newHeight, string position, bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
                Action originalAction = () => Resize(newWidth, newHeight, position, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
                AddActionToUndo(action, false, false);
            }
            transform.Resize(bitmaps, newWidth, newHeight, position);
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
            Center();
        }

        private void CenterAlligment_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
            Action originalAction = () => CenterAlligment(selectedBitmapIndexes, true);
            Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
            AddActionToUndo(action, false, true);
            CenterAlligment(selectedBitmapIndexes);
        }

        private void CenterAlligment(List<int> selectedBitmapIndexes, bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<WriteableBitmap> oldBitmaps = new List<WriteableBitmap>(bitmaps);
                Action originalAction = () => CenterAlligment(selectedBitmapIndexes, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, originalAction, true);
                AddActionToUndo(action, false, false);
            }

            transform.CenterAlligment(selectedBitmapIndexes, bitmaps);
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
        }

        private void ReplaceBitmaps(List<WriteableBitmap> oldBitmaps, Action originalAction, bool generateInverseAction = false) 
        {
            if (generateInverseAction == true) 
            {
                AddActionToRedo(originalAction);
            }
            bitmaps = oldBitmaps;
            currentBitmap = oldBitmaps[currentBitmapIndex];
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
            Center();
        }

        //Color palette buttons
        private void ColorPaletteAdd_Click(object sender, RoutedEventArgs e)
        {
            if (colorPalette.Contains(colorSelector.SelectedColor) == false)
            {
                colorPalette.Add(colorSelector.SelectedColor);
                AddColorPaletteButton(colorSelector.SelectedColor);
            }
        }

        private void AddColorPaletteButton(Color color)
        {
            System.Windows.Controls.Button newButton = new System.Windows.Controls.Button();
            newButton.Width = 40;
            newButton.Height = 40;
            newButton.Margin = new Thickness(2);
            WriteableBitmap bitmap = BitmapFactory.New(1, 1);
            imageManipulation.AddPixel(0, 0, color, bitmap);
            newButton.Content = new Image
            {
                Source = bitmap,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                Height = 40,
                Width = 40,
                ToolTip = color
            };
            newButton.Name = "_" + color.ToString().Substring(1);
            newButton.PreviewMouseDown += new MouseButtonEventHandler(ColorPaletteButton_Click);
            colorList.Children.Add(newButton);
        }

        private void ColorPaletteButton_Click(object sender, MouseButtonEventArgs e)
        {
            currentColorLeftButton = (System.Windows.Controls.Button)sender;
            string buttonName = "#" + currentColorLeftButton.Name.Substring(1);
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
            currentColors[colorIndex] = (Color)ColorConverter.ConvertFromString(buttonName);
            colorSelector.SelectedColor = currentColors[0];
            colorSelector.SecondaryColor = currentColors[1];
        }

        private void ColorPaletteRemove_Click(object sender, RoutedEventArgs e)
        {
            if (currentColorLeftButton != null)
            {
                string buttonName = "#" + currentColorLeftButton.Name.Substring(1);
                Color color = (Color)ColorConverter.ConvertFromString(buttonName);
                colorPalette.Remove(color);
                colorList.Children.Remove(currentColorLeftButton);
                currentColorLeftButton = null;
            }
        }

        private void ExportColorPalette_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap newBitmap = BitmapFactory.New(colorPalette.Count, 1);
            using (newBitmap.GetBitmapContext())
            {
                for (int i = 0; i < colorPalette.Count; i++)
                {
                    imageManipulation.AddPixel(i, 0, colorPalette[i], newBitmap);
                }
            }

            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(newBitmap);
        }

        private void ImportColorPalette_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "png images *(.png)|*.png;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var filePath = dialog.FileName;
                    BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(bitmapImage);
                    if (newBitmap.PixelHeight < 2 && newBitmap.PixelWidth < 257)
                    {
                        colorPalette.Clear();
                        colorList.Children.Clear();
                        using (newBitmap.GetBitmapContext())
                        {
                            for (int i = 0; i < newBitmap.PixelWidth; i++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, 0, newBitmap);
                                colorPalette.Add(color);
                                AddColorPaletteButton(color);
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }

        //Canvas controls
        private void Center_Click(object sender, RoutedEventArgs e)
        {
            Center();
        }

        private void Center()
        {
            MatrixTransform transform = new MatrixTransform();
            Matrix matrix = transform.Matrix;
            double scale = (height >= width) ? grid.ActualHeight / height : grid.ActualWidth / width;

            matrix.ScaleAtPrepend(scale, scale, 0, 0);
            transform.Matrix = matrix;

            currentScale = scale;
            if (currentScale.ToString().Length > 5 && currentScale.ToString().Contains(",")) LabelScale.Content = currentScale.ToString().Substring(0, 5);
            else LabelScale.Content = currentScale.ToString();

            paintSurface.RenderTransform = transform;
            Grid_TranslateTransform.X = 0;
            Grid_TranslateTransform.Y = 0;
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UIElement element = (UIElement)sender;
            Point position = e.GetPosition(element);
            MatrixTransform transform = (MatrixTransform)paintSurface.RenderTransform;
            Matrix matrix = transform.Matrix;

            // Pokud je e >= 0 dojde k přibližování
            double scale = (e.Delta >= 0) ? 1.1 : 0.9;

            matrix.ScaleAtPrepend(scale, scale, mousePosition.X - paintSurface.Width / 2, mousePosition.Y - paintSurface.Height / 2);
            transform.Matrix = matrix;

            currentScale *= scale;
            if (currentScale.ToString().Length > 5 && currentScale.ToString().Contains(",")) LabelScale.Content = currentScale.ToString().Substring(0, 5);
            else LabelScale.Content = currentScale.ToString();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                gridDragStartPoint = e.GetPosition(window);
                gridDragOffset = new System.Windows.Vector(Grid_TranslateTransform.X, Grid_TranslateTransform.Y);
                grid.CaptureMouse();
            }
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (grid.IsMouseCaptured)
            {
                System.Windows.Vector offset = Point.Subtract(e.GetPosition(window), gridDragStartPoint);
                Grid_TranslateTransform.X = gridDragOffset.X + offset.X;
                Grid_TranslateTransform.Y = gridDragOffset.Y + offset.Y;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle) grid.ReleaseMouseCapture();
        }

        //Animation controls
        private void CreateImage_Click(object sender, RoutedEventArgs e)
        {
            int bitmapIndex = currentBitmapIndex + 1;
            WriteableBitmap newBitmap = BitmapFactory.New(width, height);
            CreateNewFrame(newBitmap, currentBitmapIndex + 1);
            Action action = () => DeleteImage(bitmapIndex, true, false, newBitmap);
            AddActionToUndo(action, false, true);
        }

        private void DuplicateImage_Click(object sender, RoutedEventArgs e)
        {
            int bitmapIndex = currentBitmapIndex + 1;
            WriteableBitmap newBitmap = bitmaps[currentBitmapIndex].Clone();
            CreateNewFrame(newBitmap, currentBitmapIndex + 1);
            Action action = () => DeleteImage(bitmapIndex, true, false, newBitmap);
            AddActionToUndo(action, false, true);
        }

        private void MergeImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                int bitmapIndex = currentBitmapIndex + 1;
                WriteableBitmap newBitmap = filters.MergeImages(currentBitmap, bitmaps[currentBitmapIndex + 1], width, height);
                CreateNewFrame(newBitmap, currentBitmapIndex + 1);
                Action action = () => DeleteImage(bitmapIndex, true, false, newBitmap);
                AddActionToUndo(action, false, true);
            }
        }

        private void IntersectImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                int bitmapIndex = currentBitmapIndex + 1;
                WriteableBitmap newBitmap = filters.IntersectImages(currentBitmap, bitmaps[currentBitmapIndex + 1], width, height);
                CreateNewFrame(newBitmap, bitmapIndex);
                Action action = () => DeleteImage(bitmapIndex, true, false, newBitmap);
                AddActionToUndo(action, false, true);
            }
        }

        private void CreateNewFrame(WriteableBitmap newWriteableBitmap, int bitmapIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => DeleteImage(bitmapIndex, false, true, newWriteableBitmap);
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => DeleteImage(bitmapIndex, true, false, newWriteableBitmap);
                AddActionToUndo(action, false, false);
            }

            if (bitmaps.Count > 0) 
            {
                intersectButton.IsEnabled = true;
                mergeButton.IsEnabled = true;
            }

            for (int i = 0; i < layers.Count; i++) 
            {
                if (i != currentLayerIndex) 
                {
                    List<WriteableBitmap> layer = layers[i];
                    layer.Insert(bitmapIndex, newWriteableBitmap);
                }
            }
            UpdateLayerPreviewButtons();

            bitmaps.Insert(bitmapIndex, newWriteableBitmap);
            currentBitmapIndex = bitmapIndex;
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
            UpdateImagePreviewButtons();
            if (onionSkinning == true) UpdateOnionSkinning();
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap currentBitmap = new WriteableBitmap(bitmaps[currentBitmapIndex]);
            int bitmapIndex = currentBitmapIndex;
            DeleteImage(bitmapIndex);
            
            Action action = () => CreateNewFrame(currentBitmap, bitmapIndex, true, false);
            AddActionToUndo(action, false, true);
        }

        private void DeleteImage(int bitmapIndex, bool generateInverseAction = false, bool generateAction = false, WriteableBitmap bitmap = null)
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => CreateNewFrame(bitmap, bitmapIndex, false, true);
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true) 
            {
                Action action = () => CreateNewFrame(bitmap, bitmapIndex, true, false);
                AddActionToUndo(action, false, false);
            }

            bitmaps.RemoveAt(bitmapIndex);
            if (bitmaps.Count == 0)
            {
                WriteableBitmap newWriteableBitmap = BitmapFactory.New(width, height);
                bitmaps.Add(newWriteableBitmap);
            }
            else if (bitmaps.Count > 1)
            {
                intersectButton.IsEnabled = true;
                mergeButton.IsEnabled = true;
            }

            //pokud je poslední index vrátit se na předchozí obrázek
            int lastIndex = bitmapIndex != bitmaps.Count ? 0 : 1;
            currentBitmapIndex -= lastIndex;
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
            UpdateImagePreviewButtons();
            if (onionSkinning == true) UpdateOnionSkinning();
        }

        private void FramesPerSecond_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            double value = slider.Value;
            LabelFramesPerSecond.Content = value.ToString();
            currentFPSTarget = (int)value;
            if (currentFPSTarget != 0)
            {
                timerInterval = 1000 / currentFPSTarget;
                timer.Stop();
                timer.Interval = timerInterval;
                if (playAnimation == true) timer.Start();
            }
            else
            {
                timer.Stop();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (playAnimation == true)
            {
                BitmapImage image = new BitmapImage(new Uri("Files/Images/Dark-theme/play.png", UriKind.Relative));
                playImage.Source = image;
                playAnimation = false;
                timer.Stop();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
            else
            {
                BitmapImage image = new BitmapImage(new Uri("Files/Images/Dark-theme/pause.png", UriKind.Relative));
                playImage.Source = image;
                playAnimation = true;
                timer.Start();
            }
        }

        private void UpdateImagePreviewButtons()
        {
            previewButtons = ImagePreviews.Children.OfType<System.Windows.Controls.Button>().ToList();
            foreach (System.Windows.Controls.Button button in previewButtons)
            {
                ImagePreviews.Children.Remove(button);
            }

            for (int i = 0; i < bitmaps.Count; i++)
            {
                System.Windows.Controls.Button newButton = new System.Windows.Controls.Button();
                var brush = new ImageBrush();
                brush.ImageSource = bitmaps[i];

                Image newImage = new Image
                {
                    Source = bitmaps[i],
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.Uniform,
                    Height = 120,
                    Width = 120,
                };

                RenderOptions.SetBitmapScalingMode(newImage, BitmapScalingMode.NearestNeighbor);
                newButton.Content = newImage;
                newButton.IsEnabled = (currentBitmapIndex == i) ? false : true;
                newButton.Width = 140;
                newButton.Height = 140;
                newButton.Name = "ImagePreview" + i.ToString();
                //newButton.SetResourceReference(System.Windows.Controls.Control.StyleProperty, "AnimationButton");
                newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(PreviewButton_Click));
                ImagePreviews.Children.Add(newButton);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((System.Windows.Controls.Button)sender).Name;
            int index = int.Parse(buttonName.Replace("ImagePreview", ""));
            currentBitmapIndex = index;
            UpdateCurrentBitmap(currentBitmapIndex);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            PreviousImage();
        }

        private void PreviousImage()
        {
            if (currentBitmapIndex - 1 > -1)
            {
                currentBitmapIndex -= 1;
                UpdateCurrentBitmap(currentBitmapIndex);
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextImage();
        }

        private void NextImage()
        {
            if (currentBitmapIndex + 1 < bitmaps.Count)
            {
                currentBitmapIndex += 1;
                UpdateCurrentBitmap(currentBitmapIndex);
            }
        }

        private void UpdateCurrentBitmap(int bitmapIndex)
        {
            width = bitmaps[0].PixelWidth;
            height = bitmaps[0].PixelHeight;
            paintSurface.Width = width;
            paintSurface.Height = height;
            image.Width = width;
            image.Height = height;
            previewBitmap = BitmapFactory.New(width, height);
            previewImage.Source = previewBitmap;
            previewImage.Width = width;
            previewImage.Height = height;
            if (width != bitmaps[0].PixelWidth && height != bitmaps[0].PixelHeight) Center();
            currentBitmapIndex = bitmapIndex;
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
            if (onionSkinning == true) UpdateOnionSkinning();
            previewButtons = ImagePreviews.Children.OfType<System.Windows.Controls.Button>().ToList();
            for (int i = 0; i < previewButtons.Count; i++)
            {
                if (currentBitmapIndex == i) previewButtons[i].IsEnabled = false;
                else previewButtons[i].IsEnabled = true;
            }
            UpdateLayerViewing();
        }

        private void UpdateOnionSkinning()
        {
            RemoveOnionSkinning();
            if (currentBitmapIndex > 0)
            {
                Image previousBitmap = new Image();
                previousBitmap.Source = bitmaps[currentBitmapIndex - 1];
                previousBitmap.Opacity = 0.25f;
                RenderOptions.SetBitmapScalingMode(previousBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(previousBitmap);
            }

            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                Image nextBitmap = new Image();
                nextBitmap.Source = bitmaps[currentBitmapIndex + 1];
                nextBitmap.Opacity = 0.25f;
                RenderOptions.SetBitmapScalingMode(nextBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(nextBitmap);
            }
        }

        private void RemoveOnionSkinning()
        {
            List<Image> children = paintSurface.Children.OfType<Image>().ToList();
            foreach (Image child in children)
            {
                if (child != image && child != previewImage) paintSurface.Children.Remove(child);
            }
        }

        private void OnionSkinning_Checked(object sender, RoutedEventArgs e)
        {
            onionSkinning = true;
            UpdateOnionSkinning();
        }

        private void OnionSkinning_Unchecked(object sender, RoutedEventArgs e)
        {
            onionSkinning = false;
            RemoveOnionSkinning();
        }

        private void OnionSkinning_Click(object sender, RoutedEventArgs e) 
        {
            if (onionSkinning)
            {
                onionSkinning = false;
                RemoveOnionSkinning();
            }
            else 
            {
                onionSkinning = true;
                UpdateOnionSkinning();
            } 
        }

        //Menu controls
        private void ExportSingle_Click(object sender, RoutedEventArgs e)
        {
            ExportSingle();
        }

        private void ExportSingle()
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(currentBitmap);
        }

        private void ExportFull_Click(object sender, RoutedEventArgs e)
        {
            ExportFull();
        }

        private void ExportFull()
        {
            WriteableBitmap finalBitmap = transform.CreateCompositeBitmap(bitmaps);
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(finalBitmap);
        }

        private void ExportGif_CLick(object sender, RoutedEventArgs e)
        {
            ExportGif();
        }

        private void ExportGif()
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportGif(bitmaps, timerInterval);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void Import()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "png images *(.png)|*.png;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var filePath = dialog.FileName;
                    BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(bitmapImage);
                    WindowLoadImage subwindow = new WindowLoadImage();
                    subwindow.ShowDialog();

                    if (subwindow.importImage || subwindow.importSpritesheet)
                    {
                        if (subwindow.importImage == true)
                        {
                            currentBitmap = newBitmap;
                            bitmaps[0] = currentBitmap;
                            //Clear nesmí být použito protože to potom vytváří chybu v OnTimedEvent
                            for (int i = 1; i < bitmaps.Count; i++)
                            {
                                bitmaps.RemoveAt(i);
                            }
                        }
                        else
                        {
                            //Vydělení strany animace velikostí snímku
                            int rows = newBitmap.PixelWidth / subwindow.imageWidth;
                            int columns = newBitmap.PixelHeight / subwindow.imageHeight;
                            int offsetWidth = subwindow.offsetWidth;
                            int offsetHeight = subwindow.offsetWidth;

                            bitmaps.Clear();

                            //Získání jednotlivých snímků 
                            for (int j = 0; j < rows; j++)
                            {
                                for (int i = 0; i < columns; i++)
                                {
                                    Int32Rect rect = new Int32Rect(i * subwindow.imageWidth, j * subwindow.imageHeight, subwindow.imageWidth, subwindow.imageHeight);
                                    CroppedBitmap croppedBitmap = new CroppedBitmap(newBitmap, rect);
                                    WriteableBitmap writeableBitmap = new WriteableBitmap(croppedBitmap);
                                    bitmaps.Add(writeableBitmap);
                                }
                            }
                            currentBitmap = bitmaps[0];
                        }

                        currentBitmapIndex = 0;
                        UpdateImagePreviewButtons();
                        UpdateCurrentBitmap(currentBitmapIndex);
                        LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
                        LabelPosition.Content = "[" + width + ":" + height + "] " + mousePosition.X + ":" + mousePosition.Y;
                    }

                }
                catch
                {

                }
            }
        }

        private void ExitApp_CLick(object sender, RoutedEventArgs e)
        {
            //Přidat "are you sure?"
            System.Windows.Application.Current.Shutdown();
        }

        //Undo/redo
        private void AddActionToUndo(Action action, bool performAction, bool clearRedoStack)
        {
            if (performAction == true) action();
            undoStack.Push(action);
            if(clearRedoStack == true) redoStack.Clear();
        }

        private void AddActionToRedo(Action action)
        {
            redoStack.Push(action);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                if (visitedPoints.Count != 0) visitedPoints.Clear();
                undoStack.Pop().Invoke();
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex);
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                if (visitedPoints.Count != 0) visitedPoints.Clear();
                redoStack.Pop().Invoke();
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex);
            }
        }

        //Keyboard shortcuts
        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Misc
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control) Undo();
            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) Redo();
            if (e.Key == Key.Tab) Center();
            if (e.Key == Key.Down) NextImage();
            if (e.Key == Key.Up) PreviousImage();
            if (e.Key == Key.Subtract) DeleteImage(currentBitmapIndex);
            if (e.Key == Key.Add) CreateNewFrame(BitmapFactory.New(width, height), currentBitmapIndex + 1);

            //Storage
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) ExportFull();
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.Modifiers == ModifierKeys.Shift) ExportSingle();
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) ExportGif();
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) Import();

            //Tools
            if (e.Key == Key.P) DisableToolButton(ToolSelection.brush);
            if (e.Key == Key.E) DisableToolButton(ToolSelection.eraser);
            if (e.Key == Key.O) DisableToolButton(ToolSelection.colorPicker);
            if (e.Key == Key.C) DisableToolButton(ToolSelection.ellipse);
            if (e.Key == Key.R) DisableToolButton(ToolSelection.rectangle);
            if (e.Key == Key.L) DisableToolButton(ToolSelection.line);
            if (e.Key == Key.K) DisableToolButton(ToolSelection.path);
            if (e.Key == Key.B) DisableToolButton(ToolSelection.bucket);
            if (e.Key == Key.A) DisableToolButton(ToolSelection.specialBucket);
            if (e.Key == Key.U) DisableToolButton(ToolSelection.shading);
            if (e.Key == Key.D) DisableToolButton(ToolSelection.dithering);
            if (e.Key == Key.V) DisableToolButton(ToolSelection.symmetricBrush);
        }

        private void DisableToolButton(ToolSelection newTool)
        {
            currentTool = newTool;
            string buttonName = currentTool.ToString();
            List<System.Windows.Controls.Button> children = toolButtons.Children.OfType<System.Windows.Controls.Button>().ToList();
            foreach (System.Windows.Controls.Button child in children)
            {
                if (child.Name == buttonName)
                {
                    child.IsEnabled = false;
                    if (lastToolButton != null) lastToolButton.IsEnabled = true;
                    lastToolButton = child;
                }
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Center();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Center();
        }

        private void CreateLayer_Click(object sender, RoutedEventArgs e)
        {
            currentLayerIndex++;
            int layerIndex = currentLayerIndex;
            CreateLayer(currentLayerIndex);
            Action action = () => DeleteLayer(layerIndex++, true, false);
            AddActionToUndo(action, false, true);
        }

        private void CreateLayer(int layerIndex, bool generateInverseAction = false, bool generateAction = false) 
        {
            /*if (generateInverseAction == true)
            {
                Action inverseAction = () => 
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => 
                AddActionToUndo(action, false, false);
            }*/
            currentLayerIndex = layerIndex;
            List<WriteableBitmap> newLayer = new List<WriteableBitmap>();
            foreach (WriteableBitmap bitmap in bitmaps)
            {
                WriteableBitmap newBitamp = BitmapFactory.New(width, height);
                newLayer.Add(newBitamp);
            }
            layers.Insert(currentLayerIndex, newLayer);
            bitmaps = newLayer;
            
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
            UpdateLayerPreviewButtons();
        }

        private void UpdateLayerPreviewButtons()
        {
            layerButtons = LayerPreviews.Children.OfType<System.Windows.Controls.Button>().ToList();
            foreach (System.Windows.Controls.Button button in layerButtons)
            {
                LayerPreviews.Children.Remove(button);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                System.Windows.Controls.Button newButton = new System.Windows.Controls.Button();
                newButton.Content = i + 1;
                newButton.IsEnabled = (currentLayerIndex == i) ? false : true;
                newButton.Width = 140;
                newButton.Height = 40;
                newButton.Name = "LayerPreview" + i.ToString();
                //newButton.SetResourceReference(System.Windows.Controls.Control.StyleProperty, "AnimationButton");
                newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(LayerButton_Click));
                LayerPreviews.Children.Add(newButton);
            }

            UpdateLayerViewing();
        }

        private void LayerButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((System.Windows.Controls.Button)sender).Name;
            int index = int.Parse(buttonName.Replace("LayerPreview", ""));
            currentLayerIndex = index;
            bitmaps = layers[index];
            UpdateCurrentBitmap(currentBitmapIndex);
            UpdateImagePreviewButtons();
            UpdateLayerViewing();
            UpdateLayerPreviewButtons();
        }

        private void UpdateLayerViewing()
        {
            RemoveLayers();
            for (int i = 0; i < layers.Count; i++) 
            {
                if (i != currentLayerIndex) 
                {
                    Image layerBitmap = new Image();
                    List<WriteableBitmap> layer = layers[i];
                    layerBitmap.Source = layer[currentBitmapIndex];
                    layerBitmap.Opacity = 0.25f;
                    RenderOptions.SetBitmapScalingMode(layerBitmap, BitmapScalingMode.NearestNeighbor);
                    paintSurface.Children.Add(layerBitmap);
                }
            }
        }

        private void RemoveLayers()
        {
            List<Image> children = paintSurface.Children.OfType<Image>().ToList();
            foreach (Image child in children)
            {
                if (child != image && child != previewImage) paintSurface.Children.Remove(child);
            }
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            int layerIndex = currentLayerIndex;
            DeleteLayer(layerIndex);
            Action action = () => CreateLayer(layerIndex, true, false);
            AddActionToUndo(action, false, true);
        }

        private void DeleteLayer(int layerIndex, bool generateInverseAction = false, bool generateAction = false) 
        {
            /*if (generateInverseAction == true)
            {
                Action inverseAction = () => DeleteImage(bitmapIndex, false, true, newWriteableBitmap);
                AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => DeleteImage(bitmapIndex, true, false, newWriteableBitmap);
                AddActionToUndo(action, false, false);
            }*/

            if (layerIndex > 0)
            {
                layers.RemoveAt(layerIndex);
                layerIndex--;
                bitmaps = layers[layerIndex];
                currentLayerIndex = layerIndex;
                UpdateCurrentBitmap(currentBitmapIndex);
                UpdateImagePreviewButtons();
                UpdateLayerPreviewButtons();
            }
        }

        private void DuplicateLayer_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}