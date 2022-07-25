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

        private Color[] currentColors;
        private int currentColorIndex = 0;
        private int currentStrokeThickness = 1;
        private bool alphaBlending;
        private bool ctrl;
        private bool shift;
        private enum ToolSelection { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipse, shading, rectangle, dithering, move, path, rectangleSelection, lassoSelection, shapeSelection };
        private ToolSelection currentTool = ToolSelection.brush;

        private Point gridDragStartPoint;
        private Vector gridDragOffset;
        private double currentScale = 1.0;

        private System.Drawing.Point mousePosition = new System.Drawing.Point(0, 0);
        private System.Drawing.Point mouseDownPosition = new System.Drawing.Point(-1, -1);
        private List<System.Drawing.Point> undoPoints = new List<System.Drawing.Point>();
        private List<Color> undoColors = new List<Color>();
        private System.Drawing.Point previousMousePoint = new System.Drawing.Point();

        private List<Image> onionSkinningImages = new List<Image>();
        private bool onionSkinning;
        private Button lastToolButton;
        private Button currentColorLeftButton;

        private Transforms transforms = new Transforms();
        private Filters filters = new Filters();
        private UndoRedoStack undoRedoStack = new UndoRedoStack();
        private Tools tools = new Tools();
        private FileManagement fileManagement = new FileManagement();
        private ColorPalette colorPalette = new ColorPalette();
        private Preview preview = new Preview();

        public MainWindow()
        {
            InitializeComponent();
            this.Show();
            this.StateChanged += new EventHandler(Window_StateChanged);
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);

            currentColors = new Color[4] { colorSelector.SelectedColor, colorSelector.SecondaryColor, Color.FromArgb(255, 178, 213, 226), Color.FromArgb(0, 0, 0, 0) };
            currentBitmap = BitmapFactory.New(64, 64);
            layers.Add(new List<WriteableBitmap>());    //Výchozí vrstva
            layers[0].Add(currentBitmap);               //Výchozí snímek v animaci 

            LabelScale.Content = "1.0";
            lastToolButton = brush;

            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateLayerPreviewButtons();
            UpdateImagePreviewButtons();
            Center();
            CreatePreviews();
            preview.Initialization(layers, layerPreviewImages);
        }

        //Aktualizace plátna
        private void UpdateCurrentBitmap(int bitmapIndex, int layerIndex)
        {
            if (width != layers[0][0].PixelWidth && height != layers[0][0].PixelHeight) Center();
            width = layers[0][0].PixelWidth;
            height = layers[0][0].PixelHeight;
            if (bitmapIndex < 0) bitmapIndex = 0;
            currentBitmapIndex = bitmapIndex;
            if (layerIndex < 0) layerIndex = 0;
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
            preview.Update(layers, currentBitmapIndex);
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
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) 
            {
                currentColors[1] = colorSelector.SecondaryColor;
                currentColors[0] = colorSelector.SelectedColor;
                currentColorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                mousePosition = new System.Drawing.Point((int)e.GetPosition(image).X, (int)e.GetPosition(image).Y);
                preview.StopAnimation(currentBitmapIndex);

                switch (currentTool)
                {
                    case ToolSelection.brush:
                        {
                            tools.DefualtBrush(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.symmetricBrush:
                        {
                            tools.Symmetric(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints);
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
                            currentColors[currentColorIndex] = currentBitmap.GetPixel(mousePosition.X, mousePosition.Y);
                            if (currentColorIndex == 0) colorSelector.SelectedColor = currentColors[0];
                            else colorSelector.SecondaryColor = currentColors[1];
                            break;
                        }
                    case ToolSelection.dithering:
                        {
                            tools.Dithering(currentBitmap, mousePosition, currentColors[0], currentColors[1], currentStrokeThickness, alphaBlending, undoPoints, undoColors);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.shading:
                        {
                            tools.Shading(currentBitmap, mousePosition, ctrl, currentStrokeThickness, undoPoints, undoColors);
                            previousMousePoint = mousePosition;
                            break;
                        }
                    case ToolSelection.bucket:
                        {
                            tools.FloodFill(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, undoPoints, undoColors);
                            break;
                        }
                    case ToolSelection.specialBucket:
                        {
                            tools.ColorReplacement(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, undoPoints, undoColors);
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
                    default: break;
                }
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            mousePosition = new System.Drawing.Point((int)e.GetPosition(image).X, (int)e.GetPosition(image).Y);
            LabelPosition.Content = "[" + width + ":" + height + "] " + mousePosition.X + ":" + mousePosition.Y;

            if (mousePosition.X >= 0 && mousePosition.Y >= 0 && mousePosition.X < width && mousePosition.Y < height)
            {
                previewBitmap.Clear();

                switch (currentTool)
                {
                    case ToolSelection.brush:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.DefualtBrush(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.symmetricBrush:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.Symmetric(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.eraser:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.DefualtBrush(currentBitmap, mousePosition, currentColors[3], false, currentStrokeThickness, undoColors, undoPoints, previousMousePoint);
                                previousMousePoint = mousePosition;
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);

                            break;
                        }
                    case ToolSelection.colorPicker:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                currentColors[currentColorIndex] = currentBitmap.GetPixel(mousePosition.X, mousePosition.Y);
                                if (currentColorIndex == 0) colorSelector.SelectedColor = currentColors[0];
                                else colorSelector.SecondaryColor = currentColors[1];
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, 1, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.dithering:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.Dithering(currentBitmap, mousePosition, currentColors[0], currentColors[1], currentStrokeThickness, alphaBlending, undoPoints, undoColors);
                                previousMousePoint = mousePosition;
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.shading:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.Shading(currentBitmap, mousePosition, ctrl, currentStrokeThickness, undoPoints, undoColors);
                                previousMousePoint = mousePosition;
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.bucket:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.FloodFill(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, undoPoints, undoColors);
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, 1, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.specialBucket:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                tools.ColorReplacement(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, undoPoints, undoColors);
                                preview.StopAnimation(currentBitmapIndex);
                            }
                            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, 1, undoColors, undoPoints, true);
                            break;
                        }
                    case ToolSelection.line:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                            {
                                tools.Line(previewBitmap, mouseDownPosition, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, shift, ctrl, true);
                            }
                            else
                            {
                                tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            }
                            break;
                        }
                    case ToolSelection.path:
                        {
                            if (mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                            {
                                if (ctrl) //Pokud uživatel drží ctrl začne se nová cesta
                                    tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                                else
                                    tools.Line(previewBitmap, mouseDownPosition, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, shift, ctrl, true);
                            }
                            else
                            {
                                tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            }
                            break;
                        }
                    case ToolSelection.ellipse:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                            {
                                tools.Ellipse(previewBitmap, mouseDownPosition, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, shift, ctrl, true);
                            }
                            else
                            {
                                tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            }
                            break;
                        }
                    case ToolSelection.rectangle:
                        {
                            if ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) && mouseDownPosition.X != -1 && mouseDownPosition.Y != -1)
                            {
                                tools.Rectangle(previewBitmap, mouseDownPosition, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, shift, ctrl, true);
                            }
                            else
                            {
                                tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, currentStrokeThickness, undoColors, undoPoints, true);
                            }
                            break;
                        }
                    default: break;
                }
            }
        }

        private void Image_MouseUp(object sender, MouseEventArgs e)
        {
            mousePosition = new System.Drawing.Point((int)e.GetPosition(image).X, (int)e.GetPosition(image).Y);
            preview.PlayAnimation();

            switch (currentTool)
            {
                case ToolSelection.line:
                    {
                        tools.Line(currentBitmap, mouseDownPosition, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, shift, ctrl);
                        break;
                    }
                case ToolSelection.path:
                    {
                        if (ctrl)
                            tools.StrokeThicknessSetter(currentBitmap, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints);
                        else
                            tools.Line(currentBitmap, mouseDownPosition, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, shift, ctrl);
                        break;
                    }
                case ToolSelection.ellipse:
                    {
                        tools.Ellipse(currentBitmap, mouseDownPosition, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, shift, ctrl);
                        break;
                    }
                case ToolSelection.rectangle:
                    {
                        tools.Rectangle(currentBitmap, mouseDownPosition, mousePosition, currentColors[currentColorIndex], alphaBlending, currentStrokeThickness, undoColors, undoPoints, shift, ctrl);
                        break;
                    }
                default: break;
            }

            List<System.Drawing.Point> points = new List<System.Drawing.Point>(undoPoints);
            List<Color> colors = new List<Color>(undoColors);
            int bitmapIndex = currentBitmapIndex;
            int layerIndex = currentLayerIndex;
            Action action = () => GenerateInversePoints(points, colors, bitmapIndex, layerIndex, true);
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
            int previewStrokeThickness = (currentTool == ToolSelection.specialBucket || currentTool == ToolSelection.bucket || currentTool == ToolSelection.colorPicker) ? 1 : currentStrokeThickness;
            tools.StrokeThicknessSetter(previewBitmap, mousePosition, currentColors[2], false, previewStrokeThickness, undoColors, undoPoints, true);
        }

        private void GenerateInversePoints(List<System.Drawing.Point> points, List<Color> colors, int bitmapIndex, int layerIndex, bool generateInverseAction = false, bool generateAction = false)
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
                Action action = () => GenerateInversePoints(redoPoints, redoColors, bitmapIndex, layerIndex, false, true);
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
                Action action = () => GenerateInversePoints(undoPoints, undoColors, bitmapIndex, layerIndex, true, false);
                undoRedoStack.AddActionToUndo(action, false);
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (colors.Count != 1 && colors.Count == points.Count) bitmap.SetPixel(points[i].X, points[i].Y, colors[i]);
                else bitmap.SetPixel(points[i].X, points[i].Y, colors[0]);
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
            for (int i = 0; i < onionSkinningImages.Count; i++)
            {
                onionSkinningImages[i].Source = null;
            }
            if (onionSkinning == true) 
            {
                if (currentBitmapIndex > 0) onionSkinningImages[0].Source = layers[currentLayerIndex][currentBitmapIndex - 1];
                if (currentBitmapIndex < layers[currentLayerIndex].Count - 1) onionSkinningImages[1].Source = layers[currentLayerIndex][currentBitmapIndex + 1];
            }
        }

        private void OnionSkinning_Click(object sender, RoutedEventArgs e)
        {
            onionSkinning = !onionSkinning;
            UpdateOnionSkinning();
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
            if (colorPalette.AddColor(colorSelector.SelectedColor))
            {
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
                colorPalette.RemoveColor(color);
                colorList.Children.Remove(currentColorLeftButton);
                currentColorLeftButton = null;
            }
        }

        private void ExportColorPalette_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap newBitmap = BitmapFactory.New(colorPalette.Colors.Count, 1);
            using (newBitmap.GetBitmapContext())
            {
                for (int i = 0; i < colorPalette.Colors.Count; i++)
                {
                    newBitmap.SetPixel(i, 0, colorPalette.Colors[i]);
                }
            }
            fileManagement.ExportImage(new List<WriteableBitmap>() { newBitmap }, 0, true);
        }

        private void ImportColorPalette_Click(object sender, RoutedEventArgs e)
        {
            List<Color> colors = fileManagement.ImportColorPalette();
            if (colors != null) 
            {
                colorPalette.RemoveAllColors();
                colorList.Children.Clear();
                for (int i = 0; i < colors.Count; i++)
                {
                    colorPalette.AddColor(colors[i]);
                    AddColorPaletteButton(colors[i]);
                }
            }
        }

        //Zobrazení výsledné animace
        private void FramesPerSecond_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            LabelFramesPerSecond.Content = slider.Value.ToString();
            preview.FramesPerSecond((int)slider.Value, currentBitmapIndex);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage image;
            if (preview.Play(currentBitmapIndex) == true)
            {
                image = new BitmapImage(new Uri("Files/Images/Dark-theme/pause.png", UriKind.Relative));
            }
            else
            {
                image = new BitmapImage(new Uri("Files/Images/Dark-theme/play.png", UriKind.Relative));
            }
            playImage.Source = image;
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

            DeleteImageButton.IsEnabled = true;

            
            for (int i = 0; i < layers.Count; i++) 
            {
                layers[i].Insert(bitmapIndex, layer[i]);
            }
            
            currentBitmapIndex = bitmapIndex;
            preview.SetPreviewAnimationImages(currentBitmapIndex);
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
            preview.SetPreviewAnimationImages(currentBitmapIndex);
            UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            UpdateImagePreviewButtons();
        }

        private void UpdateImagePreviewButtons()
        {
            previewButtons = ImagePreviews.Children.OfType<Button>().ToList();
            foreach (Button button in previewButtons)
            {
                button.Content = null;
            }

            //Pokud se počet tlačítek nerovná počtu snímků musíme jej vyrovnat
            if (previewButtons.Count < layers[currentLayerIndex].Count)
            {
                //Přidat tlačítka
                ControlCreator controlCreator = new ControlCreator();
                for (int i = previewButtons.Count; i < layers[currentLayerIndex].Count; i++) 
                {
                    Button newButton = controlCreator.ImagePreviewButton(i, new RoutedEventHandler(PreviewButton_Click));
                    previewButtons.Add(newButton);
                    ImagePreviews.Children.Add(newButton);
                }
            }
            else if (previewButtons.Count > layers[currentLayerIndex].Count)
            {
                //Odstranit tlačítka
                for (int i = previewButtons.Count - 1; i >= layers[currentLayerIndex].Count; i--) 
                {
                    Console.WriteLine(i);
                    ImagePreviews.Children.Remove(previewButtons[i]);
                }
            }

            for (int i = 0; i < layers[currentLayerIndex].Count; i++)
            {
                Image newImage = new Image
                {
                    Source = layers[currentLayerIndex][i],
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.Uniform,
                    Height = 120,
                    Width = 120,
                };
                previewButtons[i].Content = newImage;
                previewButtons[i].IsEnabled = (currentBitmapIndex == i) ? false : true;
            }

            if (layers[0].Count > 1) { 
                IntersectButton.IsEnabled = true;
                MergeButton.IsEnabled = true;
                DeleteImageButton.IsEnabled = true;
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
                button.Content = null;
            }

            //Pokud se počet tlačítek nerovná počtu snímků musíme jej vyrovnat
            if (layerButtons.Count < layers.Count)
            {
                //Přidat tlačítka
                ControlCreator controlCreator = new ControlCreator();
                for (int i = layerButtons.Count; i < layers.Count; i++)
                {
                    Button newButton = controlCreator.LayerPreviewButton(i, currentLayerIndex, new RoutedEventHandler(LayerButton_Click));
                    layerButtons.Add(newButton);
                    LayerPreviews.Children.Add(newButton);
                }
            }
            else if (layerButtons.Count > layers.Count)
            {
                //Odstranit tlačítka
                for (int i = layerButtons.Count - 1; i >= layers.Count; i--)
                {
                    LayerPreviews.Children.Remove(layerButtons[i]);
                }
            }

            for (int i = 0; i < layers.Count; i++)
            {
                layerButtons[i].Content = i + 1;
                layerButtons[i].IsEnabled = (currentLayerIndex == i) ? false : true;
            }

            if (layers.Count > 1)
            {
                DeleteLayerButton.IsEnabled = true;
                if (currentLayerIndex == 0) MoveLayerUpButton.IsEnabled = false;
                else MoveLayerUpButton.IsEnabled = true;
                if (currentLayerIndex == layers.Count - 1) MoveLayerDownButton.IsEnabled = false;
                else MoveLayerDownButton.IsEnabled = true;
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
        private void NewAnimation_Click(object sender, RoutedEventArgs e)
        {
            WindowStartup windowStartup = new WindowStartup();
            windowStartup.ShowDialog();
            if (windowStartup.newHeight > 0 && windowStartup.newWidth > 0) 
            {
                List<List<WriteableBitmap>> newLayers = new List<List<WriteableBitmap>>();
                newLayers.Add(new List<WriteableBitmap>());
                newLayers[0].Add(BitmapFactory.New(windowStartup.newWidth, windowStartup.newWidth));
                layers = new List<List<WriteableBitmap>>(newLayers);
                currentBitmapIndex = 0;
                currentLayerIndex = 0;
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                undoRedoStack.ClearStacks();
                Center();
            }
        }

        private void SaveAnimation_Click(object sender, RoutedEventArgs e) 
        {
            SaveAnimation();
        }

        private void SaveAnimation() 
        {
            fileManagement.SaveFile(layers, width, height);
        }

        private void LoadAnimation_Click(object sender, RoutedEventArgs e)
        {
            LoadAnimation();
        }

        private void LoadAnimation() 
        {
            List<List<WriteableBitmap>> newLayers = fileManagement.LoadFile();
            if (newLayers != null)
            {
                layers = new List<List<WriteableBitmap>>(newLayers);
                currentBitmapIndex = 0;
                currentLayerIndex = 0;
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
                undoRedoStack.ClearStacks();
                Center();
            }
        }

        private void ExportAnimation_Click(object sender, RoutedEventArgs e)
        {
            ExportAnimation();
        }

        private void ExportAnimation()
        {
            List<WriteableBitmap> combinedBitmaps = new List<WriteableBitmap>();
            for (int i = 0; i < layers[0].Count; i++)
            {
                WriteableBitmap combinedBitmap = filters.MergeAllLayers(layers, i, width, height);
                combinedBitmaps.Add(combinedBitmap);
            }
            fileManagement.ExportImage(combinedBitmaps, preview.timerInterval);
        }

        private void ImportAnimation_Click(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void Import()
        {
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

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            undoPoints.Clear();
            if (undoRedoStack.Redo() == true)
            {
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            undoPoints.Clear();
            if (undoRedoStack.Undo() == true)
            {
                UpdateImagePreviewButtons();
                UpdateCurrentBitmap(currentBitmapIndex, currentLayerIndex);
            }
        }

        //Nastavení okna
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl) ctrl = true;
            if (e.Key == Key.LeftShift) shift = true;

            //Klávesové zkratky
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

            //Storage
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) SaveAnimation();
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) ExportAnimation();

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

            if (e.Key == Key.NumPad1) currentStrokeThickness = 1;
            if (e.Key == Key.NumPad2) currentStrokeThickness = 2;
            if (e.Key == Key.NumPad3) currentStrokeThickness = 3;
            if (e.Key == Key.NumPad4) currentStrokeThickness = 4;
            if (e.Key == Key.NumPad5) currentStrokeThickness = 5;
            if (e.Key == Key.NumPad6) currentStrokeThickness = 6;
            if (e.Key == Key.NumPad7) currentStrokeThickness = 7;
            if (e.Key == Key.NumPad8) currentStrokeThickness = 8;
            if (e.Key == Key.NumPad9) currentStrokeThickness = 9;
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl) ctrl = false;
            if (e.Key == Key.LeftShift) shift = false;
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

        //Vycentrování plátna při manipulaci s oknem 
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