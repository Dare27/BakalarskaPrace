﻿using System;
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
        private readonly List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
        private List<System.Windows.Controls.Button> previewButtons = new List<System.Windows.Controls.Button>();
        private readonly WriteableBitmap defaultBitmap;
        WriteableBitmap previewBitmap;
        Color[] currentColors;
        List<Color> colorPalette = new List<Color>();
        int currentColorIndex = 0;
        int strokeThickness = 1;
        bool alphaBlending;
        Color seedColor;
        enum toolSelection { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipse, shading, rectangle, dithering, move, path };
        toolSelection currentTool = toolSelection.brush;
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

        private List<Action> undoStack = new List<Action>();
        private List<Action> redoStack = new List<Action>();

        public MainWindow()
        {
            imageManipulation = new ImageManipulation();
            geometry = new Geometry();
            transform = new Transform();
            tools = new Tools();
            filters = new Filters();

            InitializeComponent();
            this.Show();
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
            defaultBitmap = BitmapFactory.New(width, height);

            previewBitmap = defaultBitmap.Clone();
            previewImage.Source = previewBitmap;
            previewImage.Width = width;
            previewImage.Height = height;

            currentBitmap = defaultBitmap.Clone();
            bitmaps.Add(currentBitmap);
            paintSurface.Width = width;
            paintSurface.Height = height;
            image.Width = width;
            image.Height = height;
            image.Source = bitmaps[currentBitmapIndex];
            LabelPosition.Content = "[" + width + ":" + height + "] " + 0 + ":" + 0;
            LabelScale.Content = "1.0";
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
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

            mousePosition = e.GetPosition(image);

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                currentColorIndex = colorIndex;

                switch (currentTool)
                {
                    case toolSelection.brush:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            GeneratePoints(drawPoints, currentColors[colorIndex], alphaBlending, currentBitmap, strokeThickness);
                            break;
                        }   
                    case toolSelection.symmetricBrush:
                        {
                            drawPoints = tools.SymmetricDrawing(x, y, currentBitmap);
                            GeneratePoints(drawPoints, currentColors[colorIndex], alphaBlending, currentBitmap, strokeThickness);
                            break;
                        }
                    case toolSelection.eraser:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            GeneratePoints(drawPoints, currentColors[3], false, currentBitmap, strokeThickness);
                            break;
                        }
                    case toolSelection.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case toolSelection.bucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            tools.FloodFill(x, y, currentColors[colorIndex], seedColor, currentBitmap, alphaBlending);
                            break;
                        }
                    case toolSelection.specialBucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            List<WriteableBitmap> selectedBitmaps = new List<WriteableBitmap>();
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) selectedBitmaps = bitmaps;
                            else selectedBitmaps.Add(currentBitmap);
                            tools.SpecialBucket(bitmaps, currentColors[colorIndex], seedColor, alphaBlending);
                            break;
                        }
                    case toolSelection.line:
                        {
                            mouseDownPosition.X = x;
                            mouseDownPosition.Y = y;
                            break;
                        }
                    case toolSelection.path:
                        {
                            if (mouseDownPosition.X == -1 && mouseDownPosition.Y == -1) 
                            {
                                mouseDownPosition.X = x;
                                mouseDownPosition.Y = y;
                            }
                            break;
                        }
                    case toolSelection.ellipse:
                        {
                            mouseDownPosition.X = x;
                            mouseDownPosition.Y = y;
                            break;
                        }
                    case toolSelection.rectangle:
                        {
                            mouseDownPosition.X = x;
                            mouseDownPosition.Y = y;
                            break;
                        }
                    case toolSelection.dithering:
                        {
                            drawPoints = new List<Point>() { new Point(x, y) };
                            tools.Dithering(x, y, currentColors[0], currentColors[1], currentBitmap);
                            break;
                        }
                    case toolSelection.shading:
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                //Zesvětlení
                                tools.Lighten(x, y, currentBitmap);
                            }
                            else
                            {
                                //Ztmavení 
                                tools.Darken(x, y, currentBitmap);
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

            mousePosition = e.GetPosition(image);

            LabelPosition.Content = "[" + width + ":" + height + "] " + x + ":" + y;
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case toolSelection.brush:
                        {
                            drawPoints.Add(new Point(x, y));
                            GeneratePoints(drawPoints, currentColors[colorIndex], alphaBlending, currentBitmap, strokeThickness);
                            break;
                        }
                    case toolSelection.symmetricBrush:
                        {
                            drawPoints.AddRange(tools.SymmetricDrawing(x, y, currentBitmap));
                            GeneratePoints(drawPoints, currentColors[colorIndex], alphaBlending, currentBitmap, strokeThickness);
                            break;
                        }
                    case toolSelection.eraser:
                        {
                            drawPoints.Add(new Point(x, y));
                            GeneratePoints(drawPoints, currentColors[3], false, currentBitmap, strokeThickness);
                            break;
                        }
                    case toolSelection.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case toolSelection.bucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            tools.FloodFill(x, y, currentColors[colorIndex], seedColor, currentBitmap, alphaBlending);
                            break;
                        }
                    case toolSelection.specialBucket:
                        {
                            seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            List<WriteableBitmap> selectedBitmaps = new List<WriteableBitmap>();
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                selectedBitmaps = bitmaps;
                            }
                            else
                            {
                                selectedBitmaps.Add(currentBitmap);
                            }
                            tools.SpecialBucket(selectedBitmaps, currentColors[colorIndex], seedColor, alphaBlending);
                            break;
                        }
                    case toolSelection.dithering:
                        {
                            drawPoints.Add(new Point(x, y));
                            tools.Dithering(x, y, currentColors[0], currentColors[1], currentBitmap);
                            break;
                        }
                    case toolSelection.shading:
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                //Zesvětlení
                                tools.Lighten(x, y, currentBitmap);
                            }
                            else
                            {
                                //Ztmavení 
                                tools.Darken(x, y, currentBitmap);
                            }
                            break;
                        }
                    default: break;
                }
            }

            if (x >= 0 && y >= 0 && x < width && y < height && (previewMousePosition.X != x || previewMousePosition.Y != y))
            {
                previewBitmap.Clear();
                previewMousePoints.Clear();

                bool fill;
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    fill = true;
                }
                else
                {
                    fill = false;
                }

                previewMousePosition.X = x;
                previewMousePosition.Y = y;

                switch (currentTool)
                {
                    case toolSelection.line:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                {
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                    {
                                        previewMousePoints.AddRange(geometry.DrawStraightLine((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, width, height));
                                    }
                                    else
                                    {
                                        previewMousePoints.AddRange(geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y));
                                    }
                                }
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case toolSelection.path:
                        {
                            if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                            {
                                //Pokud uživatel drzží ctrl začne se nová cesta
                                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                {
                                    previewMousePoints.Add(previewMousePosition);
                                }
                                else
                                {
                                    previewMousePoints.AddRange(geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y));
                                }
                            }
                            else 
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case toolSelection.ellipse:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                {
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                    {
                                        previewMousePoints.AddRange(geometry.DrawEllipse(mouseDownPosition, e.GetPosition(image), true, fill));
                                    }
                                    else
                                    {
                                        previewMousePoints.AddRange(geometry.DrawEllipse(mouseDownPosition, e.GetPosition(image), false, fill));
                                    }
                                }
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    case toolSelection.rectangle:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                {
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                    {
                                        previewMousePoints.AddRange(geometry.DrawRectangle(mouseDownPosition, e.GetPosition(image), true, fill));
                                    }
                                    else
                                    {
                                        previewMousePoints.AddRange(geometry.DrawRectangle(mouseDownPosition, e.GetPosition(image), false, fill));
                                    }
                                }
                            }
                            else
                            {
                                previewMousePoints.Add(previewMousePosition);
                            }
                            break;
                        }
                    default:
                        previewMousePoints.Add(previewMousePosition);
                        break;
                }

                using (previewBitmap.GetBitmapContext())
                {
                    foreach (Point point in previewMousePoints)
                    {
                        imageManipulation.AddPixel((int)point.X, (int)point.Y, currentColors[2], previewBitmap);
                    }
                }
            }
        }

        private unsafe void Image_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            mousePosition = e.GetPosition(image);

            if (visitedPoints.Count != 0) visitedPoints.Clear();

            //Musí být definována nová proměnná jinak delegát předá odkaz na aktuální 
            int thickness = strokeThickness;
            Color color = currentColors[currentColorIndex];
            Color colorSeed = seedColor;
            Color primaryColor = currentColors[0];
            Color secondaryColor = currentColors[1];
            WriteableBitmap bitmap = currentBitmap;

            switch (currentTool)
            {
                case toolSelection.brush:
                    {
                        List<Point> points = new List<Point>(drawPoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.symmetricBrush:
                    {
                        List<Point> points = new List<Point>(drawPoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.eraser:
                    {
                        List<Point> points = new List<Point>(drawPoints);
                        Action action = () => GeneratePoints(points, currentColors[3], false, bitmap, thickness);
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.bucket:
                    {
                        Action action = () => tools.FloodFill(x, y, color, colorSeed, bitmap, alphaBlending);
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.specialBucket:
                    {
                        Action action = () => tools.SpecialBucket(bitmaps, color, colorSeed, alphaBlending);
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.line:
                    {
                        List<Point> points = new List<Point>(previewMousePoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        action();
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.path:
                    {
                        List<Point> points = new List<Point>(previewMousePoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        action();
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.ellipse:
                    {
                        List<Point> points = new List<Point>(previewMousePoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        action();
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.rectangle:
                    {
                        List<Point> points = new List<Point>(previewMousePoints);
                        Action action = () => GeneratePoints(points, color, alphaBlending, bitmap, thickness);
                        action();
                        undoStack.Add(action);
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.dithering:
                    {
                        List<Point> points = new List<Point>(drawPoints);
                        foreach (Point point in points) {
                            Action action = () => tools.Dithering(x, y, primaryColor, secondaryColor, bitmap);
                            undoStack.Add(action);
                        }
                        redoStack.Clear();
                        break;
                    }
                case toolSelection.shading:
                    {
                        /*if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            //Zesvětlení
                            Action action = () => tools.Lighten(x, y, bitmap);
                            action();
                            undoStack.Add(action);
                        }
                        else
                        {
                            //Ztmavení 
                            Action action = () => tools.Darken(x, y, bitmap);
                            action();
                            undoStack.Add(action);
                        }*/
                        break;
                    }
                default: break;
            }

            drawPoints.Clear();

            if (currentTool != toolSelection.path) mouseDownPosition = new Point(-1, -1);
            else mouseDownPosition = new Point(x, y);

            previewBitmap.Clear();
            previewMousePoints.Clear();
            previewMousePoints.Add(mousePosition);
            imageManipulation.AddPixel((int)mousePosition.X, (int)mousePosition.Y, currentColors[2], previewBitmap);
        }

        private void GeneratePoints(List<Point> points, Color color, bool alphaBlend, WriteableBitmap bitmap, int thickness)
        {
            foreach (Point point in points)
            {
                StrokeThicknessSetter((int)point.X, (int)point.Y, color, alphaBlend, bitmap, thickness);
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
                        if (!visitedPoints.Contains(point))
                        {
                            visitedPoints.Add(point);
                            points.Add(point);
                        }
                    }
                }
            }

            using (bitmap.GetBitmapContext())
            {
                foreach (var point in points)
                {
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
            currentTool = (toolSelection)Enum.Parse(typeof(toolSelection), text);
            button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            if (lastToolButton != null) lastToolButton.IsEnabled = true;
            lastToolButton = button;
        }

        //Transform buttons
        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                    transform.Flip(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                        bitmaps[currentBitmapIndex] = newBitmap;
                        image.Source = currentBitmap;
                    }
                }
            }
            else
            {
                WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                transform.Flip(newBitmap, currentBitmap);
                currentBitmap = newBitmap;
                bitmaps[currentBitmapIndex] = newBitmap;
                image.Source = currentBitmap;
            }

            UpdateImagePreviewButtons();
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                int newWidth = height;
                height = width;
                width = newWidth;
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                    transform.RotateAnimation(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                    }
                }
            }
            else
            {
                WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                transform.RotateImage(newBitmap, currentBitmap);
                currentBitmap = newBitmap;
            }
            bitmaps[currentBitmapIndex] = currentBitmap;
            image.Width = currentBitmap.PixelWidth;
            image.Height = currentBitmap.PixelHeight;
            image.Source = currentBitmap;
            paintSurface.Width = currentBitmap.PixelWidth;
            paintSurface.Height = currentBitmap.PixelHeight;
            UpdateImagePreviewButtons();
        }

        private void CropToFit_Click(object sender, RoutedEventArgs e)
        {
            int leftPixelX = width;
            int rightPixelX = 0;
            int topPixelY = height;
            int downPixelY = 0;

            foreach (WriteableBitmap bitmap in bitmaps)
            {
                int currentLeftPixelX = width;
                int currentRightPixelX = 0;
                int currentTopPixelY = height;
                int currentDownPixelY = 0;

                //Projít dolu a doprava 
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
                        if (color.A != 0)
                        {
                            if (currentRightPixelX < i)
                            {
                                currentRightPixelX = i;
                            }

                            if (currentDownPixelY < j)
                            {
                                currentDownPixelY = j;
                            }
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int i = width; i >= 0; i--)
                {
                    for (int j = height; j >= 0; j--)
                    {
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
                        if (color.A != 0)
                        {
                            if (currentLeftPixelX > i)
                            {
                                currentLeftPixelX = i;
                            }

                            if (currentTopPixelY > j)
                            {
                                currentTopPixelY = j;
                            }
                        }
                    }
                }

                //Zvolit maxima
                if (currentTopPixelY < topPixelY)
                {
                    topPixelY = currentTopPixelY;
                }

                if (currentLeftPixelX < leftPixelX)
                {
                    leftPixelX = currentLeftPixelX;
                }

                if (currentRightPixelX > rightPixelX)
                {
                    rightPixelX = currentRightPixelX;
                }

                if (currentDownPixelY > downPixelY)
                {
                    downPixelY = currentDownPixelY;
                }
            }

            int newWidth = rightPixelX - leftPixelX + 1;
            int newHeight = downPixelY - topPixelY + 1;
            if (newWidth > 0 && newHeight > 0)
            {
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    WriteableBitmap newBitmap = BitmapFactory.New(newWidth, newHeight);
                    //Získání pixelů z aktuální bitmapy
                    using (newBitmap.GetBitmapContext())
                    {
                        for (int i = leftPixelX; i <= rightPixelX; i++)
                        {
                            for (int j = topPixelY; j <= downPixelY; j++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                                if (color.A != 0)
                                {
                                    //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                    imageManipulation.AddPixel(i - leftPixelX, j - topPixelY, color, newBitmap);
                                }
                            }
                        }
                    }

                    bitmaps[k] = newBitmap;
                    if (k == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                        image.Source = currentBitmap;
                    }
                }

                width = newWidth;
                height = newHeight;
                paintSurface.Width = width;
                paintSurface.Height = height;
                image.Width = width;
                image.Height = height;
                previewBitmap = BitmapFactory.New(width, height);
                previewImage.Source = previewBitmap;
                previewImage.Width = width;
                previewImage.Height = height;
                if (onionSkinning == true) UpdateOnionSkinning();
                UpdateImagePreviewButtons();
                Center();
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            WindowResize subwindow = new WindowResize();
            subwindow.ShowDialog();

            if (subwindow.newWidth != 0 && subwindow.newHeight != 0)
            {
                //Získání pixelů z aktuální bitmapy
                int croppedWidth;
                int croppedHeight;
                //startPos je souřadnice zajišťující posun do zkrácené bitmapy při zmenšení 
                int startPosX = 0;
                int startPosY = 0;
                //endPos je souřadnice zajišťující posun do finální bitmapy při zvětšení 
                int endPosX = 0;
                int endPosY = 0;

                if (subwindow.newWidth < width)
                {
                    croppedWidth = subwindow.newWidth;
                    if (subwindow.position.Contains("ĺeft")) startPosX = 0;
                    else if (subwindow.position.Contains("middle")) startPosX = (width / 2) - (subwindow.newWidth / 2);
                    else if (subwindow.position.Contains("right")) startPosX = width - subwindow.newWidth;
                }
                else
                {
                    croppedWidth = width;
                    if (subwindow.position.Contains("ĺeft")) endPosX = 0;
                    else if (subwindow.position.Contains("middle")) endPosX = (subwindow.newWidth - width) / 2;
                    else if (subwindow.position.Contains("right")) endPosX = subwindow.newWidth - width;
                }

                if (subwindow.newHeight < height)
                {
                    croppedHeight = subwindow.newHeight;
                    if (subwindow.position.Contains("top")) startPosY = 0;
                    else if (subwindow.position.Contains("middle")) startPosY = (height / 2) - (subwindow.newHeight / 2);
                    else if (subwindow.position.Contains("bottom")) startPosY = height - subwindow.newHeight;
                }
                else
                {
                    croppedHeight = height;
                    if (subwindow.position.Contains("top")) endPosY = 0;
                    else if (subwindow.position.Contains("middle")) endPosY = (subwindow.newHeight - height) / 2;
                    else if (subwindow.position.Contains("bottom")) endPosY = subwindow.newHeight - height;
                }

                Int32Rect rect = new Int32Rect(startPosX, startPosY, croppedWidth, croppedHeight);

                for (int k = 0; k < bitmaps.Count; k++)
                {
                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmaps[k], rect);
                    WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                    WriteableBitmap finalBitmap = BitmapFactory.New(subwindow.newWidth, subwindow.newHeight);

                    //Zapsání pixelů z staré bitmapy do nové
                    using (newBitmap.GetBitmapContext())
                    {
                        for (int i = 0; i < croppedWidth; i++)
                        {
                            for (int j = 0; j < croppedHeight; j++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, j, newBitmap);
                                imageManipulation.AddPixel(i + endPosX, j + endPosY, color, finalBitmap);
                            }
                        }
                    }

                    bitmaps[k] = finalBitmap;
                    if (k == currentBitmapIndex)
                    {
                        currentBitmap = finalBitmap;
                        image.Source = currentBitmap;
                    }
                }

                width = subwindow.newWidth;
                height = subwindow.newHeight;
                paintSurface.Width = width;
                paintSurface.Height = height;
                image.Width = width;
                image.Height = height;
                previewBitmap = BitmapFactory.New(width, height);
                previewImage.Source = previewBitmap;
                previewImage.Width = width;
                previewImage.Height = height;
                if (onionSkinning == true) UpdateOnionSkinning();
                UpdateImagePreviewButtons();
                Center();
            }
        }

        private void CenterAlligment_Click(object sender, RoutedEventArgs e)
        {
            List<WriteableBitmap> selectedBitmaps = new List<WriteableBitmap>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                selectedBitmaps = bitmaps;
            }
            else
            {
                selectedBitmaps.Add(currentBitmap);
            }
            Action action = () => transform.CenterAlligment(selectedBitmaps);
            action();
            undoStack.Add(action);
            redoStack.Clear();
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
            newButton.Name = color.ToString().Substring(1);
            newButton.PreviewMouseDown += new MouseButtonEventHandler(ColorPaletteButton_Click);
            colorList.Children.Add(newButton);
        }

        private void ColorPaletteButton_Click(object sender, MouseButtonEventArgs e)
        {
            var colorListButtons = colorList.Children.OfType<System.Windows.Controls.Button>();

            currentColorLeftButton = (System.Windows.Controls.Button)sender;
            string buttonName = "#" + currentColorLeftButton.Name;
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
            currentColors[colorIndex] = (Color)ColorConverter.ConvertFromString(buttonName);
            colorSelector.SelectedColor = currentColors[0];
            colorSelector.SecondaryColor = currentColors[1];
        }

        private void ColorPaletteRemove_Click(object sender, RoutedEventArgs e)
        {
            if (currentColorLeftButton != null)
            {
                string buttonName = "#" + currentColorLeftButton.Name;
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
            double scale;
            if (height >= width) scale = grid.ActualHeight / height;
            else scale = grid.ActualWidth / width;

            matrix.ScaleAtPrepend(scale, scale, 0, 0);
            transform.Matrix = matrix;

            currentScale = scale;
            if (currentScale.ToString().Length > 5 && currentScale.ToString().Contains(".")) LabelScale.Content = currentScale.ToString().Substring(0, 5);
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
            double scale = 1;

            // Pokud je e >= 0 dojde k přibližování
            if (e.Delta >= 0)
            {
                if (currentScale < 70) scale = 1.1;
            }
            else
            {
                if (currentScale > 7) scale = 0.9;
            }

            matrix.ScaleAtPrepend(scale, scale, mousePosition.X - paintSurface.Width / 2, mousePosition.Y - paintSurface.Height / 2);
            transform.Matrix = matrix;

            currentScale *= scale;
            if (currentScale.ToString().Length > 5 && currentScale.ToString().Contains(".")) LabelScale.Content = currentScale.ToString().Substring(0, 5);
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
            if (e.ChangedButton == MouseButton.Middle)
            {
                grid.ReleaseMouseCapture();
            }
        }

        //Animation controls
        private void CreateImage_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap newBitmap = BitmapFactory.New(width, height);
            CreateNewFrame(newBitmap);
        }

        private void DuplicateImage_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap newBitmap = bitmaps[currentBitmapIndex].Clone();
            CreateNewFrame(newBitmap);
        }

        private void MergeImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                WriteableBitmap newBitmap = filters.MergeImages(currentBitmap, bitmaps[currentBitmapIndex + 1], width, height);
                CreateNewFrame(newBitmap);
            }
        }

        private void IntersectImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                WriteableBitmap newBitmap = filters.IntersectImages(currentBitmap, bitmaps[currentBitmapIndex + 1], width, height);
                CreateNewFrame(newBitmap);
            }
        }

        private void CreateNewFrame(WriteableBitmap newWriteableBitmap)
        {
            bitmaps.Insert(currentBitmapIndex + 1, newWriteableBitmap);
            currentBitmapIndex += 1;
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
            bitmaps.RemoveAt(currentBitmapIndex);
            if (bitmaps.Count == 0)
            {
                WriteableBitmap newWriteableBitmap = BitmapFactory.New(width, height);
                bitmaps.Add(newWriteableBitmap);
            }
            //pokud je poslední index vrátit se na předchozí obrázek
            int lastIndex = currentBitmapIndex != bitmaps.Count ? 0 : 1;
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
                if (playAnimation == true)
                {
                    timer.Start();
                }
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
                playAnimation = false;
                timer.Stop();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
            else
            {
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
                newButton.Content = new Image
                {
                    Source = bitmaps[i],
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.Uniform,
                    Height = 140,
                    Width = 140
                };

                if (currentBitmapIndex == i) newButton.IsEnabled = false;
                else newButton.IsEnabled = true;

                newButton.Width = 140;
                newButton.Height = 140;
                newButton.Name = "ImagePreview" + i.ToString();
                newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(PreviewButton_Click));
                ImagePreviews.Children.Add(newButton);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((System.Windows.Controls.Button)sender).Name;
            int index = int.Parse(buttonName.Replace("ImagePreview", ""));
            currentBitmapIndex = index;
            UpdateCurrentBitmap();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex - 1 > -1)
            {
                currentBitmapIndex -= 1;
                UpdateCurrentBitmap();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex + 1 < bitmaps.Count)
            {
                currentBitmapIndex += 1;
                UpdateCurrentBitmap();
            }
        }

        private void UpdateCurrentBitmap()
        {
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
        }

        private void UpdateOnionSkinning()
        {
            RemoveOnionSkinning();
            if (currentBitmapIndex > 0)
            {
                Image previousBitmap = new Image();
                previousBitmap.Source = bitmaps[currentBitmapIndex - 1];
                previousBitmap.Opacity = 0.25f;
                previousBitmap.Width = currentBitmap.PixelWidth;
                previousBitmap.Height = currentBitmap.PixelHeight;
                RenderOptions.SetBitmapScalingMode(previousBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(previousBitmap);
            }

            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                Image nextBitmap = new Image();
                nextBitmap.Source = bitmaps[currentBitmapIndex + 1];
                nextBitmap.Opacity = 0.25f;
                nextBitmap.Width = currentBitmap.PixelWidth;
                nextBitmap.Height = currentBitmap.PixelHeight;
                RenderOptions.SetBitmapScalingMode(nextBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(nextBitmap);
            }
        }

        private void RemoveOnionSkinning()
        {
            List<Image> children = paintSurface.Children.OfType<Image>().ToList();
            foreach (Image child in children)
            {
                if (child != image) paintSurface.Children.Remove(child);
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

        //Menu controls
        private void ExportSingle_Click(object sender, RoutedEventArgs e)
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(currentBitmap);
        }

        private void ExportFull_Click(object sender, RoutedEventArgs e)
        {
            int finalWidth = width;
            int finalHeight = height * bitmaps.Count();
            WriteableBitmap finalBitmap = BitmapFactory.New(finalWidth, finalHeight);
            using (finalBitmap.GetBitmapContext())
            {
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                            imageManipulation.AddPixel(i, j + (k * height), color, finalBitmap);
                        }
                    }
                }
            }
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(finalBitmap);
        }

        private void ExportGif_CLick(object sender, RoutedEventArgs e)
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportGif(bitmaps, timerInterval);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
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

                        image.Source = currentBitmap;
                        width = currentBitmap.PixelWidth;
                        height = currentBitmap.PixelHeight;
                        paintSurface.Width = width;
                        paintSurface.Height = height;
                        image.Width = width;
                        image.Height = height;
                        previewBitmap = BitmapFactory.New(width, height);
                        previewImage.Source = previewBitmap;
                        previewImage.Width = width;
                        previewImage.Height = height;
                        if (onionSkinning == true) UpdateOnionSkinning();
                        UpdateImagePreviewButtons();
                        currentBitmapIndex = 0;
                        LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
                        LabelPosition.Content = "[" + width + ":" + height + "] " + mousePosition.X + ":" + mousePosition.Y;
                        UpdateCurrentBitmap();
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

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                currentBitmap.Clear();
                if (visitedPoints.Count != 0) visitedPoints.Clear();

                for (int i = 0; i < undoStack.Count - 1; i++) 
                {
                    undoStack[i].Invoke();
                }

                redoStack.Add(undoStack[undoStack.Count - 1]);
                undoStack.RemoveAt(undoStack.Count - 1);
                UpdateImagePreviewButtons();
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                currentBitmap.Clear();
                if (visitedPoints.Count != 0) visitedPoints.Clear();
                for (int i = 0; i < undoStack.Count; i++)
                {
                    undoStack[i].Invoke();
                }
                redoStack[redoStack.Count - 1].Invoke();
                undoStack.Add(redoStack[redoStack.Count - 1]);
                redoStack.RemoveAt(redoStack.Count - 1);
                UpdateImagePreviewButtons();
            }
        }

        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control) Undo();
            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) Redo();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }
    }
}