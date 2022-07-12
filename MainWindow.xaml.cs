using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BakalarskaPrace.ToolsFolder;
using BakalarskaPrace.TransformsFolder;
using BakalarskaPrace.FiltersFolder;

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {
        private List<List<WriteableBitmap>> layers = new List<List<WriteableBitmap>>();
        private WriteableBitmap currentBitmap;
        private int currentBitmapIndex = 0;
        private int currentLayerIndex = 0;
        private int width, height;
        private WriteableBitmap previewBitmap;
        private List<Button> previewButtons = new List<Button>();
        private List<Button> layerButtons = new List<Button>();
        private readonly int layersMax = 20;
        private List<Image> layerPreviewImages = new List<Image>();
        private List<Image> layerCanvasImages = new List<Image>();

        private List<Color> colorPalette = new List<Color>();
        private Color[] currentColors;
        private int currentColorIndex = 0;
        private int currentStrokeThickness = 1;
        private bool alphaBlending;

        private enum ToolSelection { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipse, shading, rectangle, dithering, move, path, rectangleSelection, lassoSelection, shapeSelection };
        private ToolSelection currentTool = ToolSelection.brush;

        private Point gridDragStartPoint;
        private Vector gridDragOffset;
        private double currentScale = 1.0;

        private System.Drawing.Point mousePosition = new System.Drawing.Point(0, 0);
        private System.Drawing.Point mouseDownPosition = new System.Drawing.Point(-1, -1);
        private System.Drawing.Point previewMousePosition = new System.Drawing.Point(0, 0);
        private List<System.Drawing.Point> previewMousePoints = new List<System.Drawing.Point>();
        private List<System.Drawing.Point> undoPoints = new List<System.Drawing.Point>();
        private List<Color> undoColors = new List<Color>();
        private List<System.Drawing.Point> clipboardPoints = new List<System.Drawing.Point>();
        private System.Drawing.Point previousMousePoint = new System.Drawing.Point();
        private bool selection;

        private List<Image> onionSkinningImages = new List<Image>();
        private bool onionSkinning;
        private Button lastToolButton;
        private Button currentColorLeftButton;

        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private int timerInterval = 1000;
        private int currentAnimationIndex;
        private int currentFPSTarget = 12;
        private bool playAnimation = true;

        private Transforms transforms = new Transforms();
        private Filters filters = new Filters();
        private UndoRedoStack undoRedoStack = new UndoRedoStack();
        private Tools tools = new Tools();

        public MainWindow()
        {
            InitializeComponent();
            this.Show();
            this.StateChanged += new EventHandler(Window_StateChanged);
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);
            paintSurface.Visibility = Visibility.Hidden;
            WindowStartup windowStartup = new WindowStartup();
            windowStartup.ShowDialog();

            currentColors = new Color[4] { colorSelector.SelectedColor, colorSelector.SecondaryColor, Color.FromArgb(255, 178, 213, 226) , Color.FromArgb(0, 0, 0, 0) };

            paintSurface.Visibility = Visibility.Visible;
            currentBitmap = BitmapFactory.New(windowStartup.newWidth, windowStartup.newHeight);
            layers.Add(new List<WriteableBitmap>()); //Výchozí vrstva
            layers[0].Add(currentBitmap);
            
            LabelScale.Content = "1.0";
            lastToolButton = brush;

            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateLayerPreviewButtons();
            UpdateImagePreviewButtons();
            Center();
            CreatePreviews();

            timerInterval = 1000 / currentFPSTarget;
            timer.Interval = timerInterval;
            timer.Enabled = true;
            timer.Tick += new EventHandler(OnTimedEvent);
        }

        //Aktualizace plátna
        private void UpdateCurrentBitmap(int bitmapIndex, int layerIndex)
        {
            if (width != layers[0][0].PixelWidth && height != layers[0][0].PixelHeight) Center();
            width = layers[0][0].PixelWidth;
            height = layers[0][0].PixelHeight;
            currentBitmapIndex = bitmapIndex;
            currentLayerIndex = layerIndex;
            currentBitmap = layers[currentLayerIndex][currentBitmapIndex];
            paintSurface.Width = width;
            paintSurface.Height = height;
            image.Width = width;
            image.Height = height;
            image.Source = currentBitmap;

            previewBitmap = BitmapFactory.New(width, height);
            previewImage.Source = previewBitmap;
            previewImage.Width = width;
            previewImage.Height = height;

            LabelImages.Content = layers[currentLayerIndex].Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            LabelPosition.Content = "[" + width + ":" + height + "] " + mousePosition.X + ":" + mousePosition.Y;
            previewButtons = ImagePreviews.Children.OfType<Button>().ToList();
            for (int i = 0; i < previewButtons.Count; i++)
            {
                if (currentBitmapIndex == i) previewButtons[i].IsEnabled = false;
                else previewButtons[i].IsEnabled = true;
            }
            if (onionSkinning == true) UpdateOnionSkinning();
            if (playAnimation == false || currentFPSTarget == 0)
            {
                currentAnimationIndex = currentBitmapIndex;
                SetPreviewAnimationImages(currentAnimationIndex);
            }
            UpdateLayerViewing();
        }

        private void CreatePreviews() 
        {
            Image previousImage = new Image();
            previousImage.Opacity = 0.25f;
            onionSkinningImages.Add(previousImage);
            paintSurface.Children.Add(previousImage);

            Image nextImage = new Image();
            nextImage.Opacity = 0.25f;
            onionSkinningImages.Add(nextImage);
            paintSurface.Children.Add(nextImage);

            for (int i = 0; i < layersMax; i++)
            {
                Image image = new Image();
                image.Width = 190;
                image.Height = 190;
                image.Name = "PreviewLayerImage" + i.ToString();
                layerPreviewImages.Add(image);
                PreviewCanvas.Children.Add(image);

                Image layerCanvasImage = new Image();
                layerCanvasImage.Opacity = 0.50f;
                layerCanvasImages.Add(layerCanvasImage);
                paintSurface.Children.Add(layerCanvasImage);
            }
        }

        //Nástro pro kreslení
        private unsafe void Image_MouseDown(object sender, MouseEventArgs e)
        {
            currentColors[1] = colorSelector.SecondaryColor;
            currentColors[0] = colorSelector.SelectedColor;
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
            bool ctrl = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? true : false;
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            undoPoints = new List<System.Drawing.Point>();
            undoColors = new List<Color>();

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                StopAnimation();
                currentColorIndex = colorIndex;

                switch (currentTool)
                {
                    case ToolSelection.brush:
                        {
                            tools.DefualtBrush(currentBitmap, mousePosition, currentColors[colorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.symmetricBrush:
                        {
                            tools.Symmetric(currentBitmap, mousePosition, currentColors[colorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.eraser:
                        {
                            tools.DefualtBrush(currentBitmap, mousePosition, currentColors[3], false, currentStrokeThickness, undoColors, undoPoints);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.colorPicker:
                        {
                            currentColors[colorIndex] = currentBitmap.GetPixel(x, y);
                            if (colorIndex == 0) colorSelector.SelectedColor = currentColors[0];
                            else colorSelector.SecondaryColor = currentColors[1];
                            break;
                        }
                    case ToolSelection.bucket:
                        {
                            tools.FloodFill(currentBitmap, mousePosition, currentColors[colorIndex], undoPoints, undoColors);
                            break;
                        }
                    case ToolSelection.specialBucket:
                        {
                            tools.ColorReplacement(currentBitmap, mousePosition, currentColors[colorIndex], undoPoints, undoColors);
                            break;
                        }
                    case ToolSelection.line:
                        {
                            mouseDownPosition = mousePosition;
                            break;
                        }
                    case ToolSelection.path:
                        {
                            if (mouseDownPosition.X == -1 && mouseDownPosition.Y == -1) mouseDownPosition = mousePosition;
                            break;
                        }
                    case ToolSelection.ellipse:
                        {
                            mouseDownPosition = mousePosition;
                            break;
                        }
                    case ToolSelection.rectangle:
                        {
                            mouseDownPosition = mousePosition;
                            break;
                        }
                    case ToolSelection.dithering:
                        {
                            tools.Dithering(currentBitmap, mousePosition, currentColors[0], currentColors[1],  currentStrokeThickness, alphaBlending, undoPoints, undoColors);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.shading:
                        {
                            tools.Shading(currentBitmap, mousePosition, ctrl, currentStrokeThickness, undoPoints, undoColors);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.rectangleSelection:
                        {
                            selection = !selection;
                            if (selection == true)
                            {
                                mouseDownPosition = mousePosition;
                                previewBitmap.Clear();
                                previewMousePoints.Clear();
                                previewMousePosition = mousePosition;
                            }
                            break;
                        }
                    default: break;
                }
            }
        }

        private unsafe void Image_MouseMove(object sender, MouseEventArgs e)
        {
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
            bool shift = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) ? true : false;
            bool ctrl = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? true : false;
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;
            mousePosition = new System.Drawing.Point(x, y);

            LabelPosition.Content = "[" + width + ":" + height + "] " + x + ":" + y;
            
            if (x >= 0 && y >= 0 && x < width && y < height && (previewMousePosition.X != x || previewMousePosition.Y != y))
            {
                if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                {
                    StopAnimation();
                    switch (currentTool)
                    {
                        case ToolSelection.brush:
                            {
                                tools.DefualtBrush(currentBitmap, mousePosition, currentColors[colorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                break;
                            }
                        case ToolSelection.symmetricBrush:
                            {
                                tools.Symmetric(currentBitmap, mousePosition, currentColors[colorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                break;
                            }
                        case ToolSelection.eraser:
                            {
                                tools.DefualtBrush(currentBitmap, mousePosition, currentColors[3], false, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                break;
                            }
                        case ToolSelection.colorPicker:
                            {
                                currentColors[colorIndex] = currentBitmap.GetPixel(x, y);
                                if (colorIndex == 0) colorSelector.SelectedColor = currentColors[0];
                                else colorSelector.SecondaryColor = currentColors[1];
                                break;
                            }
                        case ToolSelection.bucket:
                            {
                                tools.FloodFill(currentBitmap, mousePosition, currentColors[colorIndex], undoPoints, undoColors);
                                break;
                            }
                        case ToolSelection.specialBucket:
                            {
                                tools.ColorReplacement(currentBitmap, mousePosition, currentColors[colorIndex], undoPoints, undoColors);
                                break;
                            }
                        case ToolSelection.dithering:
                            {
                                tools.Dithering(currentBitmap, mousePosition, currentColors[0], currentColors[1],  currentStrokeThickness, alphaBlending, undoPoints, undoColors);
                                previousMousePoint = mousePosition;
                                break;
                            }
                        case ToolSelection.shading:
                            {
                                tools.Shading(currentBitmap, mousePosition,  ctrl, currentStrokeThickness, undoPoints, undoColors);
                                previousMousePoint = mousePosition;
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
                    previewMousePosition = mousePosition;
                }

                //Vykreslení geometrických nástrojů
                switch (currentTool)
                {
                    case ToolSelection.line:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                                previewMousePoints.AddRange(tools.Line(mousePosition, mouseDownPosition, ctrl, false, width, height));
                            else
                                previewMousePoints.Add(previewMousePosition);
                            break;
                        }
                    case ToolSelection.path:
                        {
                            if (mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                            {
                                //Pokud uživatel drží ctrl začne se nová cesta
                                if (ctrl)
                                    previewMousePoints.Add(previewMousePosition);
                                else
                                    previewMousePoints.AddRange(tools.Line(mousePosition, mouseDownPosition, false, false, width, height));
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case ToolSelection.ellipse:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                                previewMousePoints.AddRange(tools.Ellipse(mouseDownPosition, mousePosition, shift, ctrl, width, height));
                            else
                                previewMousePoints.Add(previewMousePosition);
                            break;
                        }
                    case ToolSelection.rectangle:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                                previewMousePoints.AddRange(tools.Rectangle(mouseDownPosition, mousePosition, ctrl, shift, width, height));
                            else
                                previewMousePoints.Add(previewMousePosition);
                            break;
                        }
                    case ToolSelection.rectangleSelection:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                                previewMousePoints.AddRange(tools.Rectangle(mouseDownPosition, mousePosition, false, true, width, height));
                            else
                                if (selection == false) previewMousePoints.Add(previewMousePosition);
                            break;
                        }
                    default:
                        previewMousePoints.Add(previewMousePosition);
                        break;
                }

                int previewStrokeThickness = (currentTool == ToolSelection.specialBucket || currentTool == ToolSelection.bucket || currentTool == ToolSelection.colorPicker) ? 1 : currentStrokeThickness;
                foreach (System.Drawing.Point point in previewMousePoints)
                {
                    tools.StrokeThicknessSetter(previewBitmap, point, currentColors[2], false, previewStrokeThickness, undoColors, undoPoints, true);
                }
            }
        }

        private unsafe void Image_MouseUp(object sender, MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;
            mousePosition = new System.Drawing.Point(x, y);

            int strokeThickness = currentStrokeThickness;
            Color color = Color.FromArgb(currentColors[currentColorIndex].A, currentColors[currentColorIndex].R, currentColors[currentColorIndex].G, currentColors[currentColorIndex].B);
            int bitmapIndex = currentBitmapIndex;
            int layerIndex = currentLayerIndex;
            PlayAnimation();

            switch (currentTool)
            {
                case ToolSelection.line:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, bitmapIndex, layerIndex, alphaBlending, strokeThickness);
                        break;
                    }
                case ToolSelection.path:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, bitmapIndex, layerIndex, alphaBlending, strokeThickness);
                        break;
                    }
                case ToolSelection.ellipse:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, bitmapIndex, layerIndex, alphaBlending, strokeThickness);
                        break;
                    }
                case ToolSelection.rectangle:
                    {
                        GeneratePoints(previewMousePoints, new List<Color>() { color }, bitmapIndex, layerIndex, alphaBlending, strokeThickness);
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
                List<System.Drawing.Point> points = new List<System.Drawing.Point>(undoPoints);
                List<Color> colors = new List<Color>(undoColors);
                Action action = () => GeneratePoints(points, colors, bitmapIndex, layerIndex, false, 1, true);
                undoRedoStack.AddActionToUndo(action, true);

                undoPoints.Clear();
                undoColors.Clear();

                if (currentTool != ToolSelection.path) 
                {
                    mouseDownPosition.X = -1;
                    mouseDownPosition.Y = -1;
                } 
                else mouseDownPosition = mousePosition;

                previewBitmap.Clear();
                previewMousePoints.Clear();
                previewMousePoints.Add(mousePosition);

                int previewStrokeThickness = (currentTool == ToolSelection.specialBucket || currentTool == ToolSelection.bucket || currentTool == ToolSelection.colorPicker) ? 1 : currentStrokeThickness;
                foreach (System.Drawing.Point point in previewMousePoints) 
                {
                    tools.StrokeThicknessSetter(previewBitmap, point, currentColors[2], false, previewStrokeThickness, undoColors, undoPoints, true);
                }
            }
        }

        private void GeneratePoints(List<System.Drawing.Point> points, List<Color> colors, int bitmapIndex, int layerIndex, bool alphaBlend, int thickness, bool generateInverseAction = false, bool generateAction = false)
        {
            //Pokud se nezobrazuje aktuální obrázek při undo/redo tak ho zobrazit
            if (bitmapIndex != currentBitmapIndex || layerIndex != currentLayerIndex) UpdateCurrentBitmap(bitmapIndex, layerIndex);
            
            WriteableBitmap bitmap = layers[layerIndex][bitmapIndex];

            if (generateInverseAction == true)
            {
                List<System.Drawing.Point> redoPoints = new List<System.Drawing.Point>(points);
                List<Color> redoColors = new List<Color>();

                foreach (var point in redoPoints)
                {
                    Color redoColor = bitmap.GetPixel(point.X, point.Y);
                    redoColors.Add(redoColor);
                }
                Action action = () => GeneratePoints(redoPoints, redoColors, bitmapIndex, layerIndex, false, 1, false, true);
                undoRedoStack.AddActionToRedo(action);
            }
            else if (generateAction == true) 
            {
                List<System.Drawing.Point> undoPoints = new List<System.Drawing.Point>(points);
                List<Color> undoColors = new List<Color>();

                foreach (var point in undoPoints)
                {
                    Color undoColor = bitmap.GetPixel(point.X, point.Y);
                    undoColors.Add(undoColor);
                }
                Action action = () => GeneratePoints(undoPoints, undoColors, bitmapIndex, layerIndex, false, 1, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (colors.Count != 1 && colors.Count == points.Count)
                    tools.StrokeThicknessSetter(currentBitmap, points[i], colors[i], alphaBlend, thickness, undoColors, undoPoints);
                else
                    tools.StrokeThicknessSetter(currentBitmap, points[i], colors[0], alphaBlend, thickness, undoColors, undoPoints);
            }
        }

        //Brush settings
        private void ColorChanged(object sender, RoutedEventArgs e)
        {
            currentColors[1] = colorSelector.SecondaryColor;
            currentColors[0] = colorSelector.SelectedColor;
        }

        private void BrushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentStrokeThickness = (int)e.NewValue;
        }

        private void AlphaBlending_Changed(object sender, RoutedEventArgs e)
        {
            alphaBlending = !alphaBlending;
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
            MatrixTransform transform = (MatrixTransform)paintSurface.RenderTransform;
            Matrix matrix = transform.Matrix;
            double scale = (e.Delta >= 0) ? 1.1 : 0.9; // Pokud je e >= 0 dojde k přibližování

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
                gridDragOffset = new Vector(Grid_TranslateTransform.X, Grid_TranslateTransform.Y);
                grid.CaptureMouse();
            }
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (grid.IsMouseCaptured)
            {
                Vector offset = Point.Subtract(e.GetPosition(window), gridDragStartPoint);
                Grid_TranslateTransform.X = gridDragOffset.X + offset.X;
                Grid_TranslateTransform.Y = gridDragOffset.Y + offset.Y;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle) grid.ReleaseMouseCapture();
        }

        private void UpdateOnionSkinning()
        {
            RemoveOnionSkinning();
            if (currentBitmapIndex > 0) onionSkinningImages[0].Source = layers[currentLayerIndex][currentBitmapIndex - 1];
            if (currentBitmapIndex < layers[currentLayerIndex].Count - 1) onionSkinningImages[1].Source = layers[currentLayerIndex][currentBitmapIndex + 1];
        }

        private void RemoveOnionSkinning()
        {
            for (int i = 0; i < onionSkinningImages.Count; i++)
            {
                onionSkinningImages[i].Source = null;
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

        //Tool buttons
        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            string text = ((Button)sender).Name;
            currentTool = (ToolSelection)Enum.Parse(typeof(ToolSelection), text);
            Button button = (Button)sender;
            button.IsEnabled = false;
            if (lastToolButton != null) lastToolButton.IsEnabled = true;
            lastToolButton = button;
        }

        //Transform
        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;
            bool horizontal = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? false : true;
            int layerIndex = currentLayerIndex;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) 
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < layers[currentLayerIndex].Count; i++) 
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            Flip(selectedBitmapIndexes, layerIndex, layers[layerIndex], horizontal);
            Action action = () => Flip(selectedBitmapIndexes, layerIndex, layers[layerIndex], horizontal, true);
            undoRedoStack.AddActionToUndo(action, true);
        }

        private void Flip(List<int> selectedBitmapIndexes, int layerIndex, List<WriteableBitmap> bitmaps, bool horizontal, bool generateInverseAction = false, bool generateAction = false) 
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => Flip(selectedBitmapIndexes, layerIndex, bitmaps, horizontal, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => Flip(selectedBitmapIndexes, layerIndex, bitmaps, horizontal, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }
            
            transforms.Flip(selectedBitmapIndexes, bitmaps, horizontal);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;
            bool clockwise = ((Keyboard.Modifiers & ModifierKeys.Control) != 0) ? true : false;
            int layerIndex = currentLayerIndex;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < layers[currentLayerIndex].Count; i++)
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            Rotate(selectedBitmapIndexes, layerIndex, layers[layerIndex], clockwise);
            Action action = () => Rotate(selectedBitmapIndexes, layerIndex, layers[layerIndex], !clockwise, true);
            undoRedoStack.AddActionToUndo(action, true);
        }

        private void Rotate(List<int> selectedBitmapIndexes, int layerIndex, List<WriteableBitmap> bitmaps, bool clockwise, bool generateInverseAction = false, bool generateAction = false) 
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => Rotate(selectedBitmapIndexes, layerIndex, bitmaps, !clockwise, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => Rotate(selectedBitmapIndexes, layerIndex, bitmaps, !clockwise, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            transforms.Rotate(selectedBitmapIndexes, bitmaps, clockwise);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void CropToFit_Click(object sender, RoutedEventArgs e)
        {
            List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
            foreach (List<WriteableBitmap> layer in layers)
            {
                oldBitmaps.Add(new List<WriteableBitmap>(layer));
            }
            int layerIndex = currentLayerIndex;
            int bitmapIndex = currentBitmapIndex;
            Action originalAction = () => CropToFit(true);
            Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
            undoRedoStack.AddActionToUndo(action, true);
            CropToFit();
        }

        private void CropToFit(bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
                foreach (List<WriteableBitmap> layer in layers)
                {
                    oldBitmaps.Add(new List<WriteableBitmap>(layer));
                }
                int layerIndex = currentLayerIndex;
                int bitmapIndex = currentBitmapIndex;
                Action originalAction = () => CropToFit(true);
                Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
                undoRedoStack.AddActionToUndo(action, false);
            }
            transforms.CropToFit(layers);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
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

                List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
                foreach (List<WriteableBitmap> layer in layers)
                {
                    oldBitmaps.Add(new List<WriteableBitmap>(layer));
                }
                int layerIndex = currentLayerIndex;
                int bitmapIndex = currentBitmapIndex;
                Action originalAction = () => Resize(newWidth, newHeight, position, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
                undoRedoStack.AddActionToUndo(action, true);
                Resize(newWidth, newHeight, position);
            }
        }

        private void Resize(int newWidth, int newHeight, string position, bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
                foreach (List<WriteableBitmap> layer in layers)
                {
                    oldBitmaps.Add(new List<WriteableBitmap>(layer));
                }
                int layerIndex = currentLayerIndex;
                int bitmapIndex = currentBitmapIndex;
                Action originalAction = () => Resize(newWidth, newHeight, position, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
                undoRedoStack.AddActionToUndo(action, false);
            }
            transforms.Resize(layers, newWidth, newHeight, position);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
            Center();
        }

        private void CenterAlligment_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedBitmapIndexes;
            int layerIndex = currentLayerIndex;

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                selectedBitmapIndexes = new List<int>();
                for (int i = 0; i < layers[currentLayerIndex].Count; i++)
                {
                    selectedBitmapIndexes.Add(i);
                }
            }
            else selectedBitmapIndexes = new List<int>() { currentBitmapIndex };

            List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
            foreach (List<WriteableBitmap> layer in layers)
            {
                oldBitmaps.Add(new List<WriteableBitmap>(layer));
            }
            int bitmapIndex = currentBitmapIndex;
            Action originalAction = () => CenterAlligment(selectedBitmapIndexes, layerIndex, true);
            Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
            undoRedoStack.AddActionToUndo(action, true);
            CenterAlligment(selectedBitmapIndexes, layerIndex);
        }

        private void CenterAlligment(List<int> selectedBitmapIndexes, int layerIndex, bool generateAction = false)
        {
            if (generateAction == true)
            {
                List<List<WriteableBitmap>> oldBitmaps = new List<List<WriteableBitmap>>();
                foreach (List<WriteableBitmap> layer in layers) 
                {
                    oldBitmaps.Add(new List<WriteableBitmap>(layer));
                }
                int bitmapIndex = currentBitmapIndex;
                Action originalAction = () => CenterAlligment(selectedBitmapIndexes, layerIndex, true);
                Action action = () => ReplaceBitmaps(oldBitmaps, layerIndex, bitmapIndex, originalAction, true);
                undoRedoStack.AddActionToUndo(action, false);
            }

            transforms.CenterAlligment(selectedBitmapIndexes, layers[layerIndex]);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void ReplaceBitmaps(List<List<WriteableBitmap>> oldBitmaps, int layerIndex, int bitmapIndex, Action originalAction, bool generateInverseAction = false) 
        {
            if (generateInverseAction == true) 
            {
                undoRedoStack.AddActionToRedo(originalAction);
            }

            layers = oldBitmaps;
            UpdateCurrentBitmap(bitmapIndex, layerIndex);
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
            WriteableBitmap bitmap = BitmapFactory.New(1, 1);
            bitmap.SetPixel(0, 0, color);
            ControlCreator controlCreator = new ControlCreator();
            Button newButton = controlCreator.ColorPaletteButton(bitmap, color, new MouseButtonEventHandler(ColorPaletteButton_Click));
            colorList.Children.Add(newButton);
            currentColorLeftButton = newButton;
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
                    newBitmap.SetPixel(i, 0, colorPalette[i]);
                }
            }

            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(newBitmap);
        }

        private void ImportColorPalette_Click(object sender, RoutedEventArgs e)
        {
            FileManagement fileManagement = new FileManagement();
            List<Color> colors = fileManagement.ImportColorPalette();
            if (colors != null) 
            {
                colorPalette.Clear();
                colorList.Children.Clear();
                for (int i = 0; i < colors.Count; i++)
                {
                    colorPalette.Add(colors[i]);
                    AddColorPaletteButton(colors[i]);
                }
            }
        }

        //Zobrazení výsledné animace
        private void OnTimedEvent(object sender, EventArgs e)
        {
            if (currentAnimationIndex + 1 < layers[currentLayerIndex].Count) currentAnimationIndex += 1;
            else currentAnimationIndex = 0;
            SetPreviewAnimationImages(currentAnimationIndex);
        }

        private void SetPreviewAnimationImages(int animationIndex)
        {
            for (int i = 0; i < layersMax; i++)
            {
                if (i < layers.Count) layerPreviewImages[i].Source = layers[i][animationIndex];
                else layerPreviewImages[i].Source = null;
            }
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
                PlayAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (playAnimation == true)
            {
                BitmapImage image = new BitmapImage(new Uri("Files/Images/Dark-theme/play.png", UriKind.Relative));
                playImage.Source = image;
                playAnimation = false;
                StopAnimation();
            }
            else
            {
                BitmapImage image = new BitmapImage(new Uri("Files/Images/Dark-theme/pause.png", UriKind.Relative));
                playImage.Source = image;
                playAnimation = true;
                timer.Start();
            }
        }

        private void StopAnimation() 
        {
            timer.Stop();
            currentAnimationIndex = currentBitmapIndex;
            SetPreviewAnimationImages(currentAnimationIndex);
        }

        private void PlayAnimation() 
        {
            if (playAnimation == true) timer.Start();
        }

        //Animation controls
        private void CreateImage_Click(object sender, RoutedEventArgs e)
        {
            int bitmapIndex = currentBitmapIndex + 1;
            List<WriteableBitmap> layer = new List<WriteableBitmap>();
            for (int i = 0; i < layers.Count; i++)
            {
                WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                layer.Add(newBitmap);
            }
            CreateNewFrame(layer, bitmapIndex);
            Action action = () => DeleteImage(bitmapIndex, true, false, layer);
            undoRedoStack.AddActionToUndo(action, true);
        }

        private void DuplicateImage_Click(object sender, RoutedEventArgs e)
        {
            int bitmapIndex = currentBitmapIndex + 1;
            List<WriteableBitmap> layer = new List<WriteableBitmap>();
            for (int i = 0; i < layers.Count; i++)
            {
                WriteableBitmap newBitmap = layers[i][currentBitmapIndex].Clone();
                layer.Add(newBitmap);
            }
            CreateNewFrame(layer, bitmapIndex);
            Action action = () => DeleteImage(bitmapIndex, true, false, layer);
            undoRedoStack.AddActionToUndo(action, true);
        }

        private void MergeImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < layers[currentLayerIndex].Count - 1)
            {
                int bitmapIndex = currentBitmapIndex + 1;
                List<WriteableBitmap> layer = new List<WriteableBitmap>();
                for (int i = 0; i < layers.Count; i++)
                {
                    WriteableBitmap newBitmap = filters.MergeFrames(layers[i][currentBitmapIndex], layers[i][currentBitmapIndex + 1], width, height);
                    layer.Add(newBitmap);
                }
                CreateNewFrame(layer, bitmapIndex);
                Action action = () => DeleteImage(bitmapIndex, true, false, layer);
                undoRedoStack.AddActionToUndo(action, true);
            }
        }

        private void IntersectImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < layers[currentLayerIndex].Count - 1)
            {
                int bitmapIndex = currentBitmapIndex + 1;
                List<WriteableBitmap> layer = new List<WriteableBitmap>();
                for (int i = 0; i < layers.Count; i++)
                {
                    WriteableBitmap newBitmap = filters.IntersectFrames(layers[i][currentBitmapIndex], layers[i][currentBitmapIndex + 1], width, height);
                    layer.Add(newBitmap);
                }
                CreateNewFrame(layer, bitmapIndex);
                Action action = () => DeleteImage(bitmapIndex, true, false, layer);
                undoRedoStack.AddActionToUndo(action, true);
            }
        }

        private void CreateNewFrame(List<WriteableBitmap> layer, int bitmapIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => DeleteImage(bitmapIndex, false, true, layer);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                Action action = () => DeleteImage(bitmapIndex, true, false, layer);
                undoRedoStack.AddActionToUndo(action, false);
            }

            IntersectButton.IsEnabled = true;
            MergeButton.IsEnabled = true;
            DeleteImageButton.IsEnabled = true;

            
            for (int i = 0; i < layers.Count; i++) 
            {
                layers[i].Insert(bitmapIndex, layer[i]);
            }
            
            currentBitmapIndex = bitmapIndex;
            currentAnimationIndex = currentBitmapIndex;
            SetPreviewAnimationImages(currentAnimationIndex);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
            if (currentBitmapIndex == layers[0].Count - 1)
            {
                IntersectButton.IsEnabled = false;
                MergeButton.IsEnabled = false;
            }
            else
            {
                IntersectButton.IsEnabled = true;
                MergeButton.IsEnabled = true;
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (layers[currentLayerIndex].Count == 2) 
            {
                MergeButton.IsEnabled = false;
                IntersectButton.IsEnabled = false;
                DeleteImageButton.IsEnabled = false;
            }

            if (layers[currentLayerIndex].Count != 1)
            {
                List<WriteableBitmap> layer = new List<WriteableBitmap>();
                int bitmapIndex = currentBitmapIndex;
                for (int i = 0; i < layers.Count; i++)
                {
                    layer.Add(layers[i][bitmapIndex]);
                }
                DeleteImage(bitmapIndex);

                Action action = () => CreateNewFrame(layer, bitmapIndex, true, false);
                undoRedoStack.AddActionToUndo(action, true);
            }
        }

        private void DeleteImage(int bitmapIndex, bool generateInverseAction = false, bool generateAction = false, List<WriteableBitmap> layer = null)
        {
            if (generateInverseAction == true)
            {
                Action inverseAction = () => CreateNewFrame(layer, bitmapIndex, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true) 
            {
                Action action = () => CreateNewFrame(layer, bitmapIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            for (int i = 0; i < layers.Count; i++) 
            {
                layers[i].RemoveAt(bitmapIndex);
            }

            //pokud je poslední index vrátit se na předchozí obrázek
            int lastIndex = bitmapIndex != layers[currentLayerIndex].Count ? 0 : 1;
            currentBitmapIndex -= lastIndex;
            currentAnimationIndex = currentBitmapIndex;
            SetPreviewAnimationImages(currentAnimationIndex);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void UpdateImagePreviewButtons()
        {
            previewButtons = ImagePreviews.Children.OfType<Button>().ToList();
            foreach (Button button in previewButtons)
            {
                ImagePreviews.Children.Remove(button);
            }

            for (int i = 0; i < layers[currentLayerIndex].Count; i++)
            {
                WriteableBitmap bitmap = layers[currentLayerIndex][i];
                ControlCreator controlCreator = new ControlCreator();
                Button newButton = controlCreator.ImagePreviewButton(bitmap, i, currentBitmapIndex, new RoutedEventHandler(PreviewButton_Click));
                ImagePreviews.Children.Add(newButton);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Name;
            int index = int.Parse(buttonName.Replace("ImagePreview", ""));
            currentBitmapIndex = index;
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            if (currentBitmapIndex == layers[0].Count - 1)
            {
                IntersectButton.IsEnabled = false;
                MergeButton.IsEnabled = false;
            }
            else 
            {
                IntersectButton.IsEnabled = true;
                MergeButton.IsEnabled = true;
            }
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
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextImage();
        }

        private void NextImage()
        {
            if (currentBitmapIndex + 1 < layers[currentLayerIndex].Count)
            {
                currentBitmapIndex += 1;
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex); ;
            }
        }

        //Layer controls
        private void CreateLayer_Click(object sender, RoutedEventArgs e)
        {
            if (layers.Count + 1 <= layersMax)
            {
                if (layers.Count + 1 > 1) DeleteLayerButton.IsEnabled = true;
                currentLayerIndex++;
                int layerIndex = currentLayerIndex;
                List<WriteableBitmap> newLayer = new List<WriteableBitmap>();
                foreach (WriteableBitmap bitmap in layers[0])
                {
                    WriteableBitmap newBitamp = BitmapFactory.New(width, height);
                    newLayer.Add(newBitamp);
                }
                CreateLayer(newLayer, currentLayerIndex);
                Action action = () => DeleteLayer(layerIndex++, true, false);
                undoRedoStack.AddActionToUndo(action, true);
                MoveLayerUpButton.IsEnabled = true;
            }
            else
            {
                CreateLayerButton.IsEnabled = false;
            }
        }

        private void CreateLayer(List<WriteableBitmap> newLayer, int layerIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                int newLayerIndex = layerIndex;
                Action inverseAction = () => DeleteLayer(newLayerIndex, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                int newLayerIndex = layerIndex;
                Action action = () => DeleteLayer(newLayerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }
            currentLayerIndex = layerIndex;
            layers.Insert(currentLayerIndex, newLayer);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void UpdateLayerPreviewButtons()
        {
            layerButtons = LayerPreviews.Children.OfType<Button>().ToList();
            foreach (Button button in layerButtons)
            {
                LayerPreviews.Children.Remove(button);
            }

            ControlCreator controlCreator = new ControlCreator();

            for (int i = 0; i < layers.Count; i++)
            {
                Button newButton = controlCreator.LayerPreviewButton(i, currentLayerIndex, new RoutedEventHandler(LayerButton_Click));
                LayerPreviews.Children.Add(newButton);
            }
        }

        private void LayerButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Name;
            int index = int.Parse(buttonName.Replace("LayerPreview", ""));
            currentLayerIndex = index;
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();

            if (currentLayerIndex == layers.Count - 1)
            {
                MoveLayerDownButton.IsEnabled = false;
                MoveLayerUpButton.IsEnabled = true;
            }

            if (currentLayerIndex == 0)
            {
                MoveLayerDownButton.IsEnabled = true;
                MoveLayerUpButton.IsEnabled = false;
            }
        }

        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            int layerIndex = currentLayerIndex;
            Action action = () => MoveLayerDown(layerIndex - 1, true, false);
            undoRedoStack.AddActionToUndo(action, true);
            MoveLayerUp(layerIndex);
        }

        private void MoveLayerUp(int layerIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                int newLayerIndex = layerIndex - 1;
                Action inverseAction = () => MoveLayerDown(newLayerIndex, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                int newLayerIndex = layerIndex - 1;
                Action action = () => MoveLayerDown(newLayerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            List<WriteableBitmap> currentLayer = new List<WriteableBitmap>(layers[layerIndex]);
            List<WriteableBitmap> previousLayer = new List<WriteableBitmap>(layers[layerIndex - 1]);
            layers[layerIndex] = previousLayer;
            layers[layerIndex - 1] = currentLayer;
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            int layerIndex = currentLayerIndex;
            Action action = () => MoveLayerUp(layerIndex + 1, true, false);
            undoRedoStack.AddActionToUndo(action, true);
            MoveLayerDown(layerIndex);
        }

        private void MoveLayerDown(int layerIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                int newLayerIndex = layerIndex + 1;
                Action inverseAction = () => MoveLayerUp(newLayerIndex, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                int newLayerIndex = layerIndex + 1;
                Action action = () => MoveLayerUp(newLayerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            List<WriteableBitmap> currentLayer = new List<WriteableBitmap>(layers[layerIndex]);
            List<WriteableBitmap> nextLayer = new List<WriteableBitmap>(layers[layerIndex + 1]);
            layers[layerIndex] = nextLayer;
            layers[layerIndex + 1] = currentLayer;
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void UpdateLayerViewing()
        {
            for (int i = 0; i < layerCanvasImages.Count; i++)
            {
                layerCanvasImages[i].Source = null;
                if (i < layers.Count)
                {
                    layerCanvasImages[i].Source = layers[i][currentBitmapIndex];
                }
            }
            UpdateLayerPreviewButtons();
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            int layerIndex = currentLayerIndex;
            if (layers.Count - 2 <= layersMax) CreateLayerButton.IsEnabled = true;
            if (layers.Count - 1 == 1)
            {
                MoveLayerDownButton.IsEnabled = false;
                MoveLayerUpButton.IsEnabled = false;
                DeleteLayerButton.IsEnabled = false;
            }

            if (layers.Count > 1)
            {
                List<WriteableBitmap> layer = new List<WriteableBitmap>(layers[layerIndex]);
                DeleteLayer(layerIndex);
                Action action = () => CreateLayer(layer, layerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, true);
            }
        }

        private void DeleteLayer(int layerIndex, bool generateInverseAction = false, bool generateAction = false)
        {
            if (generateInverseAction == true)
            {
                List<WriteableBitmap> bitmaps = new List<WriteableBitmap>(layers[layerIndex]);
                int newLayerIndex = layerIndex;
                Action inverseAction = () => CreateLayer(bitmaps, newLayerIndex, false, true);
                undoRedoStack.AddActionToRedo(inverseAction);
            }
            else if (generateAction == true)
            {
                List<WriteableBitmap> bitmaps = new List<WriteableBitmap>(layers[layerIndex]);
                int newLayerIndex = layerIndex;
                Action action = () => CreateLayer(bitmaps, newLayerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            layers.RemoveAt(layerIndex);
            if (layerIndex - 1 == -1) layerIndex = 0;
            else layerIndex--;
            currentLayerIndex = layerIndex;
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }


        private void MergeLayers_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DuplicateLayer_Click(object sender, RoutedEventArgs e)
        {

        }

        //Menu controls
        private void SaveFull_Click(object sender, RoutedEventArgs e) 
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.SaveFile(layers, width, height);
        }

        private void LoadFull_Click(object sender, RoutedEventArgs e)
        {
            FileManagement fileManagement = new FileManagement();
            List<List<WriteableBitmap>> newLayers = fileManagement.LoadFile();
            if (newLayers != null) 
            {
                layers = new List<List<WriteableBitmap>>(newLayers);
                currentBitmapIndex = 0;
                currentLayerIndex = 0;
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                undoRedoStack.ClearStacks();
            }
        }

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
            WriteableBitmap finalBitmap = filters.CreateCompositeBitmap(layers);
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
            fileManagement.ExportGif(layers[currentLayerIndex], timerInterval);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void Import()
        {
            FileManagement fileManagement = new FileManagement();
            List<List<WriteableBitmap>> newLayers = fileManagement.ImportFile();
            if (newLayers != null) 
            {
                layers = new List<List<WriteableBitmap>>(newLayers);
                currentBitmapIndex = 0;
                currentLayerIndex = 0;
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                undoRedoStack.ClearStacks();
            }
        }

        private void ExitApp_CLick(object sender, RoutedEventArgs e)
        {
            //Přidat "are you sure?"
            System.Windows.Application.Current.Shutdown();
        }

        //Klávesové zkratky
        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Misc
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                undoPoints.Clear();
                if (undoRedoStack.Undo() == true)
                {
                    UpdateImagePreviewButtons();
                    UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                }
            }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) 
            {
                undoPoints.Clear();
                if (undoRedoStack.Redo() == true) 
                {
                    UpdateImagePreviewButtons();
                    UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                }
            }
            if (e.Key == Key.Tab) Center();
            if (e.Key == Key.Down) NextImage();
            if (e.Key == Key.Up) PreviousImage();
            //if (e.Key == Key.Subtract) DeleteImage(currentBitmapIndex);
            //if (e.Key == Key.Add) CreateNewFrame(BitmapFactory.New(width, height), currentBitmapIndex + 1);

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
            List<Button> children = toolButtons.Children.OfType<Button>().ToList();
            foreach (Button child in children)
            {
                if (child.Name == buttonName)
                {
                    child.IsEnabled = false;
                    if (lastToolButton != null) lastToolButton.IsEnabled = true;
                    lastToolButton = child;
                }
            }
        }

        //Nastavení okna
        private void Window_StateChanged(object sender, EventArgs e)
        {
            Center();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Center();
        }
    }
}